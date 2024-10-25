using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common;
using OpenShock.Common.Authentication;
using OpenShock.Common.Hubs;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.Ota;
using OpenShock.Common.Utils;
using OpenShock.Serialization.Gateway;
using OpenShock.Serialization.Types;
using Redis.OM.Contracts;
using Semver;
using Serilog;

namespace OpenShock.LiveControlGateway.Controllers;
//TODO: Implement new keep alive ping pong machanism
/// <summary>
/// Communication with the devices aka ESP-32 micro controllers
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemas.DeviceToken)]
[ApiVersion("2")]
[Route("/{version:apiVersion}/ws/device")]
public sealed class DeviceV2Controller : DeviceControllerBase<HubToGatewayMessage, GatewayToHubMessage>
{
    private readonly IHubContext<UserHub, IUserHub> _userHubContext;
    private readonly Timer _pingTimer;
    private readonly DateTimeOffset _lastPingSent = DateTimeOffset.UtcNow;

    /// <summary>
    /// DI
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="lifetime"></param>
    /// <param name="redisConnectionProvider"></param>
    /// <param name="dbContextFactory"></param>
    /// <param name="userHubContext"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="lcgConfig"></param>
    public DeviceV2Controller(
        ILogger<DeviceV2Controller> logger,
        IHostApplicationLifetime lifetime,
        IRedisConnectionProvider redisConnectionProvider,
        IDbContextFactory<OpenShockContext> dbContextFactory,
        IHubContext<UserHub, IUserHub> userHubContext,
        IServiceProvider serviceProvider, LCGConfig lcgConfig)
        : base(logger, lifetime, HubToGatewayMessage.Serializer, GatewayToHubMessage.Serializer, redisConnectionProvider, 
            dbContextFactory, serviceProvider, lcgConfig)
    {
        _userHubContext = userHubContext;
        _pingTimer = new Timer(PingTimerElapsed, null, Constants.DevicePingInitialDelay, Constants.DevicePingPeriod);
    }

    private async void PingTimerElapsed(object? state)
    {
        try
        {
            await QueueMessage(new GatewayToHubMessage
            {
                Payload = new GatewayToHubMessagePayload(new Ping
                {
                    UnixUtcTime = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                })
            });
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error while sending ping message to device [{DeviceId}]", CurrentDevice.Id);
        }
    }

    /// <summary>
    /// The latency of the last ping
    /// </summary>
    public TimeSpan Latency { get; private set; }
    
    private OtaUpdateStatus? _lastStatus;
    
    private IUserHub HcOwner => _userHubContext.Clients.User(CurrentDevice.Owner.ToString());
    
    /// <inheritdoc />
    protected override async Task Handle(HubToGatewayMessage data)
    {
        var payload = data.Payload;

        await using var scope = ServiceProvider.CreateAsyncScope();
        var otaService = scope.ServiceProvider.GetRequiredService<IOtaService>();

        Logger.LogTrace("Received payload [{Kind}] from device [{DeviceId}]", payload.Kind, CurrentDevice.Id);
        switch (payload.Kind)
        {
            case HubToGatewayMessagePayload.ItemKind.Pong:
                await SelfOnline();
                Latency = DateTimeOffset.UtcNow - _lastPingSent;
                
                break;

            case HubToGatewayMessagePayload.ItemKind.OtaUpdateStarted:
                _lastStatus = OtaUpdateStatus.Started;
                await HcOwner.OtaInstallStarted(
                    CurrentDevice.Id,
                    payload.OtaUpdateStarted.UpdateId,
                    payload.OtaUpdateStarted.Version.ToSemVersion());
                await otaService.Started(
                    CurrentDevice.Id,
                    payload.OtaUpdateStarted.UpdateId,
                    payload.OtaUpdateStarted.Version.ToSemVersion());
                break;

            case HubToGatewayMessagePayload.ItemKind.OtaUpdateProgress:
                await HcOwner.OtaInstallProgress(
                    CurrentDevice.Id,
                    payload.OtaUpdateProgress.UpdateId,
                    payload.OtaUpdateProgress.Task,
                    payload.OtaUpdateProgress.Progress);

                if (_lastStatus == OtaUpdateStatus.Started)
                {
                    _lastStatus = OtaUpdateStatus.Running;
                    await otaService.Progress(CurrentDevice.Id, payload.OtaUpdateProgress.UpdateId);
                }

                break;

            case HubToGatewayMessagePayload.ItemKind.OtaUpdateFailed:
                await HcOwner.OtaInstallFailed(
                    CurrentDevice.Id,
                    payload.OtaUpdateFailed.UpdateId,
                    payload.OtaUpdateFailed.Fatal,
                    payload.OtaUpdateFailed.Message!);

                await otaService.Error(CurrentDevice.Id, payload.OtaUpdateFailed.UpdateId,
                    payload.OtaUpdateFailed.Fatal, payload.OtaUpdateFailed.Message!);

                _lastStatus = OtaUpdateStatus.Error;
                break;

            case HubToGatewayMessagePayload.ItemKind.BootStatus:
                if (payload.BootStatus.BootType == FirmwareBootType.NewFirmware)
                {
                    await HcOwner.OtaInstallSucceeded(
                        CurrentDevice.Id, payload.BootStatus.OtaUpdateId);

                    var test = await otaService.Success(CurrentDevice.Id, payload.BootStatus.OtaUpdateId);
                    _lastStatus = OtaUpdateStatus.Finished;
                    break;
                }

                if (payload.BootStatus.BootType == FirmwareBootType.Rollback)
                {
                    await HcOwner.OtaRollback(
                        CurrentDevice.Id, payload.BootStatus.OtaUpdateId);

                    await otaService.Error(CurrentDevice.Id, payload.BootStatus.OtaUpdateId, false,
                        "Device booted with firmware rollback");
                    _lastStatus = OtaUpdateStatus.Error;
                    break;
                }

                if (payload.BootStatus.BootType == FirmwareBootType.Normal)
                {
                    if (payload.BootStatus.OtaUpdateId == 0) break;

                    var unfinished = await otaService.UpdateUnfinished(CurrentDevice.Id,
                        payload.BootStatus.OtaUpdateId);

                    if (!unfinished) break;

                    Log.Warning("OTA update unfinished, rolling back");

                    await HcOwner.OtaRollback(
                        CurrentDevice.Id, payload.BootStatus.OtaUpdateId);

                    await otaService.Error(CurrentDevice.Id, payload.BootStatus.OtaUpdateId, false,
                        "Device booted with normal boot, update seems unfinished");
                    _lastStatus = OtaUpdateStatus.Error;
                }

                break;

            case HubToGatewayMessagePayload.ItemKind.NONE:
            default:
                Logger.LogWarning("Payload kind not defined [{Kind}]", payload.Kind);
                break;
        }
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
    public override async ValueTask DisposeControllerAsync()
    {
        await _pingTimer.DisposeAsync();
        await base.DisposeControllerAsync();
    }
}