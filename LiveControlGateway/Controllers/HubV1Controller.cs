using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OpenShock.Common.Authentication;
using OpenShock.Common.Hubs;
using OpenShock.Common.Models;
using OpenShock.Common.Services.Ota;
using OpenShock.Common.Utils;
using OpenShock.LiveControlGateway.LifetimeManager;
using OpenShock.Serialization.Deprecated.DoNotUse.V1;
using OpenShock.Serialization.Types;
using Semver;
using Serilog;

namespace OpenShock.LiveControlGateway.Controllers;

/// <summary>
/// Communication with the hubs aka ESP-32 microcontrollers
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemas.DeviceToken)]
[ApiVersion("1")]
[Route("/{version:apiVersion}/ws/device")]
public sealed class HubV1Controller : HubControllerBase<HubToGatewayMessage, GatewayToHubMessage>
{
    private readonly IHubContext<UserHub, IUserHub> _userHubContext;

    /// <summary>
    /// DI
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="lifetime"></param>
    /// <param name="hubLifetimeManager"></param>
    /// <param name="userHubContext"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="lcgConfig"></param>
    public HubV1Controller(
        ILogger<HubV1Controller> logger,
        IHostApplicationLifetime lifetime,
        HubLifetimeManager hubLifetimeManager,
        IHubContext<UserHub, IUserHub> userHubContext,
        IServiceProvider serviceProvider, LCGConfig lcgConfig)
        : base(logger, lifetime, HubToGatewayMessage.Serializer, GatewayToHubMessage.Serializer, hubLifetimeManager,
            serviceProvider, lcgConfig)
    {
        _userHubContext = userHubContext;
    }
    
    private OtaUpdateStatus? _lastStatus;
    
    private IUserHub HcOwner => _userHubContext.Clients.User(CurrentHub.Owner.ToString());
    
    /// <inheritdoc />
    protected override async Task Handle(HubToGatewayMessage data)
    {
        if(data.Payload == null) return;
        var payload = data.Payload.Value;

        await using var scope = ServiceProvider.CreateAsyncScope(); 
        var otaService = scope.ServiceProvider.GetRequiredService<IOtaService>();

        Logger.LogTrace("Received payload [{Kind}] from hub [{HubId}]", payload.Kind, CurrentHub.Id);
        switch (payload.Kind)
        {
            case HubToGatewayMessagePayload.ItemKind.KeepAlive:
                await SelfOnline(DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMilliseconds(payload.KeepAlive.Uptime)));
                break;

            case HubToGatewayMessagePayload.ItemKind.OtaInstallStarted:
                _lastStatus = OtaUpdateStatus.Started;
                await HcOwner.OtaInstallStarted(
                    CurrentHub.Id,
                    payload.OtaInstallStarted.UpdateId,
                    payload.OtaInstallStarted.Version!.ToSemVersion());
                await otaService.Started(
                    CurrentHub.Id,

                    payload.OtaInstallStarted.UpdateId,
                    payload.OtaInstallStarted.Version!.ToSemVersion());
                break;

            case HubToGatewayMessagePayload.ItemKind.OtaInstallProgress:
                await HcOwner.OtaInstallProgress(
                    CurrentHub.Id,
                    payload.OtaInstallProgress.UpdateId,
                    payload.OtaInstallProgress.Task,
                    payload.OtaInstallProgress.Progress);

                if (_lastStatus == OtaUpdateStatus.Started)
                {
                    _lastStatus = OtaUpdateStatus.Running;
                    await otaService.Progress(CurrentHub.Id, payload.OtaInstallProgress.UpdateId);
                }

                break;

            case HubToGatewayMessagePayload.ItemKind.OtaInstallFailed:
                await HcOwner.OtaInstallFailed(
                    CurrentHub.Id,
                    payload.OtaInstallFailed.UpdateId,
                    payload.OtaInstallFailed.Fatal,
                    payload.OtaInstallFailed.Message!);

                await otaService.Error(CurrentHub.Id, payload.OtaInstallFailed.UpdateId,
                    payload.OtaInstallFailed.Fatal, payload.OtaInstallFailed.Message!);

                _lastStatus = OtaUpdateStatus.Error;
                break;

            case HubToGatewayMessagePayload.ItemKind.BootStatus:
                if (payload.BootStatus.BootType == FirmwareBootType.NewFirmware)
                {
                    await HcOwner.OtaInstallSucceeded(
                        CurrentHub.Id, payload.BootStatus.OtaUpdateId);

                    var test = await otaService.Success(CurrentHub.Id, payload.BootStatus.OtaUpdateId);
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
                break;
        }
    }

    /// <inheritdoc />
    public override ValueTask Control(List<OpenShock.Serialization.Gateway.ShockerCommand> controlCommands)
        => QueueMessage(new GatewayToHubMessage
        {
            Payload = new GatewayToHubMessagePayload(new ShockerCommandList
            {
                Commands = controlCommands.Select(x => new ShockerCommand()
                {
                    Duration = x.Duration,
                    Type = x.Type,
                    Id = x.Model == Serialization.Types.ShockerModelType.Petrainer998DR ? (ushort)(x.Id >> 1) : x.Id, // Fix for old hubs, their ids was serialized wrongly in the RFTransmitter, the V1 endpoint is being phased out, so this wont stay here forever
                    Intensity = x.Intensity,
                    Model = x.Model
                }).ToList()
            })
        });

    /// <inheritdoc />
    public override ValueTask CaptivePortal(bool enable)
        => QueueMessage(new GatewayToHubMessage
        {
            Payload = new GatewayToHubMessagePayload(new CaptivePortalConfig()
            {
                Enabled = enable
            })
        });


    /// <inheritdoc />
    public override ValueTask OtaInstall(SemVersion version)
        => QueueMessage(new GatewayToHubMessage
        {
            Payload = new GatewayToHubMessagePayload(new OtaInstall
            {
                Version = version.ToSemVer()
            })
        });


}