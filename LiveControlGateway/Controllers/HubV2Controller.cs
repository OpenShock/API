using System.Diagnostics;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using OpenShock.Common.Authentication;
using OpenShock.Common.Constants;
using OpenShock.Common.Extensions;
using OpenShock.Common.Hubs;
using OpenShock.Common.Models;
using OpenShock.Common.Services.Ota;
using OpenShock.LiveControlGateway.LifetimeManager;
using OpenShock.LiveControlGateway.Options;
using OpenShock.Serialization.Gateway;
using OpenShock.Serialization.Types;
using Semver;
using Serilog;

namespace OpenShock.LiveControlGateway.Controllers;
//TODO: Implement new keep alive ping pong mechanism
/// <summary>
/// Communication with the hubs aka ESP-32 microcontrollers
/// </summary>
[ApiController]
[ApiVersion("2")]
[Route("/{version:apiVersion}/ws/hub")]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemas.HubToken)]
public sealed class HubV2Controller : HubControllerBase<HubToGatewayMessage, GatewayToHubMessage>
{
    private readonly IHubContext<UserHub, IUserHub> _userHubContext;
    private readonly Timer _pingTimer;
    private long _pingTimestamp = Stopwatch.GetTimestamp();
    private ushort _latencyMs;

    /// <summary>
    /// DI
    /// </summary>
    /// <param name="hubLifetimeManager"></param>
    /// <param name="userHubContext"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    public HubV2Controller(
        HubLifetimeManager hubLifetimeManager,
        IHubContext<UserHub, IUserHub> userHubContext,
        IServiceProvider serviceProvider,
        IOptions<LcgOptions> options,
        ILogger<HubV2Controller> logger
        )
        : base(HubToGatewayMessage.Serializer, GatewayToHubMessage.Serializer, hubLifetimeManager, serviceProvider, options, logger)
    {
        _userHubContext = userHubContext;
        _pingTimer = new Timer(PingTimerElapsed, null, Duration.DevicePingInitialDelay, Duration.DevicePingPeriod);
    }

    private async void PingTimerElapsed(object? state)
    {
        try
        {
            _pingTimestamp = Stopwatch.GetTimestamp();
            await QueueMessage(new GatewayToHubMessage
            {
                Payload = new GatewayToHubMessagePayload(new Ping
                {
                    UnixUtcTime = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                })
            });
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error while sending ping message to hub [{HubId}]", CurrentHub.Id);
        }
    }
    
    private OtaUpdateStatus? _lastStatus;
    
    private IUserHub HcOwner => _userHubContext.Clients.User(CurrentHub.OwnerId.ToString());
    
    /// <inheritdoc />
    protected override async Task<bool> Handle(HubToGatewayMessage data)
    {
        var payload = data.Payload;

        await using var scope = ServiceProvider.CreateAsyncScope();
        var otaService = scope.ServiceProvider.GetRequiredService<IOtaService>();

        Logger.LogTrace("Received payload [{Kind}] from hub [{HubId}]", payload.Kind, CurrentHub.Id);
        switch (payload.Kind)
        {
            case HubToGatewayMessagePayload.ItemKind.Pong:
                
                // Received pong without sending ping, this could be abusing the pong endpoint.
                if (_pingTimestamp == 0)
                {
                    // TODO: Kick or warn client.
                    return false;
                }
                
                _latencyMs = (ushort)Math.Min(Stopwatch.GetElapsedTime(_pingTimestamp).TotalMilliseconds, ushort.MaxValue); // If someone has a ping higher than 65 seconds, they are messing with us. Cap it to 65 seconds
                _pingTimestamp = 0;
                if (!await SelfOnline(payload.Pong.Uptime, _latencyMs, payload.Pong.Rssi))
                {
                    return false;
                }
                break;

            case HubToGatewayMessagePayload.ItemKind.OtaUpdateStarted:
                _lastStatus = OtaUpdateStatus.Started;
                await HcOwner.OtaInstallStarted(
                    CurrentHub.Id,
                    payload.OtaUpdateStarted.UpdateId,
                    payload.OtaUpdateStarted.Version.ToSemVersion());
                await otaService.Started(
                    CurrentHub.Id,
                    payload.OtaUpdateStarted.UpdateId,
                    payload.OtaUpdateStarted.Version.ToSemVersion());
                break;

            case HubToGatewayMessagePayload.ItemKind.OtaUpdateProgress:
                await HcOwner.OtaInstallProgress(
                    CurrentHub.Id,
                    payload.OtaUpdateProgress.UpdateId,
                    payload.OtaUpdateProgress.Task,
                    payload.OtaUpdateProgress.Progress);

                if (_lastStatus == OtaUpdateStatus.Started)
                {
                    _lastStatus = OtaUpdateStatus.Running;
                    await otaService.Progress(CurrentHub.Id, payload.OtaUpdateProgress.UpdateId);
                }

                break;

            case HubToGatewayMessagePayload.ItemKind.OtaUpdateFailed:
                await HcOwner.OtaInstallFailed(
                    CurrentHub.Id,
                    payload.OtaUpdateFailed.UpdateId,
                    payload.OtaUpdateFailed.Fatal,
                    payload.OtaUpdateFailed.Message!);

                await otaService.Error(CurrentHub.Id, payload.OtaUpdateFailed.UpdateId,
                    payload.OtaUpdateFailed.Fatal, payload.OtaUpdateFailed.Message!);

                _lastStatus = OtaUpdateStatus.Error;
                break;

            case HubToGatewayMessagePayload.ItemKind.BootStatus:
                if (payload.BootStatus.BootType == FirmwareBootType.NewFirmware)
                {
                    await HcOwner.OtaInstallSucceeded(
                        CurrentHub.Id, payload.BootStatus.OtaUpdateId);

                    await otaService.Success(CurrentHub.Id, payload.BootStatus.OtaUpdateId);
                    _lastStatus = OtaUpdateStatus.Finished;
                    break;
                }

                if (payload.BootStatus.BootType == FirmwareBootType.Rollback)
                {
                    await HcOwner.OtaRollback(
                        CurrentHub.Id, payload.BootStatus.OtaUpdateId);

                    await otaService.Error(CurrentHub.Id, payload.BootStatus.OtaUpdateId, false,
                        "Hub booted with firmware rollback");
                    _lastStatus = OtaUpdateStatus.Error;
                    break;
                }

                if (payload.BootStatus.BootType == FirmwareBootType.Normal)
                {
                    if (payload.BootStatus.OtaUpdateId == 0) break;

                    var unfinished = await otaService.UpdateUnfinished(CurrentHub.Id,
                        payload.BootStatus.OtaUpdateId);

                    if (!unfinished) break;

                    Log.Warning("OTA update unfinished, rolling back");

                    await HcOwner.OtaRollback(
                        CurrentHub.Id, payload.BootStatus.OtaUpdateId);

                    await otaService.Error(CurrentHub.Id, payload.BootStatus.OtaUpdateId, false,
                        "Hub booted with normal boot, update seems unfinished");
                    _lastStatus = OtaUpdateStatus.Error;
                }

                break;

            case HubToGatewayMessagePayload.ItemKind.NONE:
            default:
                Logger.LogWarning("Payload kind not defined [{Kind}]", payload.Kind);
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public override ValueTask Control(List<ShockerCommand> controlCommands)
        => QueueMessage(new GatewayToHubMessage
        {
            Payload = new GatewayToHubMessagePayload(new ShockerCommandList
            {
                Commands = controlCommands
            })
        });

    /// <inheritdoc />
    public override ValueTask CaptivePortal(bool enable)
        => QueueMessage(new GatewayToHubMessage
        {
            Payload = new GatewayToHubMessagePayload(new Trigger
            {
                Type = enable ? TriggerType.CaptivePortalEnable : TriggerType.CaptivePortalDisable
            })
        });

    /// <inheritdoc />
    public override ValueTask OtaInstall(SemVersion version)
        => QueueMessage(new GatewayToHubMessage
        {
            Payload = new GatewayToHubMessagePayload(new OtaUpdateRequest
            {
                Version = version.ToSemVer()
            })
        });


    /// <inheritdoc />
    protected override async ValueTask DisposeControllerAsync()
    {
        await _pingTimer.DisposeAsync();
        await base.DisposeControllerAsync();
    }
}