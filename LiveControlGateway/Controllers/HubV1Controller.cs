using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OpenShock.Common.Authentication;
using OpenShock.Common.Hubs;
using OpenShock.Common.Models;
using OpenShock.Common.Services.Ota;
using OpenShock.LiveControlGateway.LifetimeManager;
using OpenShock.LiveControlGateway.Options;
using OpenShock.Serialization.Deprecated.DoNotUse.V1;
using OpenShock.Serialization.Types;
using Serilog;

namespace OpenShock.LiveControlGateway.Controllers;

/// <summary>
/// Communication with the hubs aka ESP-32 microcontrollers
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("/{version:apiVersion}/ws/device")]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemes.HubToken)]
public sealed class HubV1Controller : HubControllerBase<HubToGatewayMessage, GatewayToHubMessage>
{
    private readonly IHubContext<UserHub, IUserHub> _userHubContext;

    /// <summary>
    /// DI
    /// </summary>
    /// <param name="hubLifetimeManager"></param>
    /// <param name="userHubContext"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    public HubV1Controller(
            HubLifetimeManager hubLifetimeManager,
            IHubContext<UserHub, IUserHub> userHubContext,
            IServiceProvider serviceProvider,
            LcgOptions options,
            ILogger<HubV1Controller> logger
        )
        : base(HubToGatewayMessage.Serializer, GatewayToHubMessage.Serializer, hubLifetimeManager, serviceProvider, options, logger)
    {
        _userHubContext = userHubContext;
    }
    
    private OtaUpdateStatus? _lastStatus;
    
    private IUserHub HcOwner => _userHubContext.Clients.User(CurrentHubOwnerId.ToString());
    
    /// <inheritdoc />
    protected override async Task<bool> Handle(HubToGatewayMessage data)
    {
        if(!data.Payload.HasValue) return false;
        var payload = data.Payload.Value;

        await using var scope = ServiceProvider.CreateAsyncScope(); 
        var otaService = scope.ServiceProvider.GetRequiredService<IOtaService>();

        Logger.LogTrace("Received payload [{Kind}] from hub [{HubId}]", payload.Kind, CurrentHubId);
        switch (payload.Kind)
        {
            case HubToGatewayMessagePayload.ItemKind.KeepAlive:
                if (!await SelfOnline(payload.KeepAlive.Uptime))
                {
                    return false;
                }
                break;

            case HubToGatewayMessagePayload.ItemKind.OtaInstallStarted:
                _lastStatus = OtaUpdateStatus.Started;
                await HcOwner.OtaInstallStarted(
                    CurrentHubId,
                    payload.OtaInstallStarted.UpdateId,
                    SemVersion.FromFbs(payload.OtaInstallStarted.Version!));
                await otaService.Started(
                    CurrentHubId,

                    payload.OtaInstallStarted.UpdateId,
                    SemVersion.FromFbs(payload.OtaInstallStarted.Version!));
                break;

            case HubToGatewayMessagePayload.ItemKind.OtaInstallProgress:
                await HcOwner.OtaInstallProgress(
                    CurrentHubId,
                    payload.OtaInstallProgress.UpdateId,
                    payload.OtaInstallProgress.Task,
                    payload.OtaInstallProgress.Progress);

                if (_lastStatus == OtaUpdateStatus.Started)
                {
                    _lastStatus = OtaUpdateStatus.Running;
                    await otaService.Progress(CurrentHubId, payload.OtaInstallProgress.UpdateId);
                }

                break;

            case HubToGatewayMessagePayload.ItemKind.OtaInstallFailed:
                await HcOwner.OtaInstallFailed(
                    CurrentHubId,
                    payload.OtaInstallFailed.UpdateId,
                    payload.OtaInstallFailed.Fatal,
                    payload.OtaInstallFailed.Message!);

                await otaService.Error(CurrentHubId, payload.OtaInstallFailed.UpdateId,
                    payload.OtaInstallFailed.Fatal, payload.OtaInstallFailed.Message!);

                _lastStatus = OtaUpdateStatus.Error;
                break;

            case HubToGatewayMessagePayload.ItemKind.BootStatus:
                if (payload.BootStatus.BootType == FirmwareBootType.NewFirmware)
                {
                    await HcOwner.OtaInstallSucceeded(
                        CurrentHubId, payload.BootStatus.OtaUpdateId);

                    await otaService.Success(CurrentHubId, payload.BootStatus.OtaUpdateId);
                    _lastStatus = OtaUpdateStatus.Finished;
                    break;
                }

                if (payload.BootStatus.BootType == FirmwareBootType.Rollback)
                {
                    await HcOwner.OtaRollback(
                        CurrentHubId, payload.BootStatus.OtaUpdateId);

                    await otaService.Error(CurrentHubId, payload.BootStatus.OtaUpdateId, false,
                        "Hub booted with firmware rollback");
                    _lastStatus = OtaUpdateStatus.Error;
                    break;
                }

                if (payload.BootStatus.BootType == FirmwareBootType.Normal)
                {
                    if (payload.BootStatus.OtaUpdateId == 0) break;

                    var unfinished = await otaService.UpdateUnfinished(CurrentHubId,
                        payload.BootStatus.OtaUpdateId);

                    if (!unfinished) break;

                    Log.Warning("OTA update unfinished, rolling back");

                    await HcOwner.OtaRollback(
                        CurrentHubId, payload.BootStatus.OtaUpdateId);

                    await otaService.Error(CurrentHubId, payload.BootStatus.OtaUpdateId, false,
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
    [NonAction]
    public override ValueTask Control(IList<OpenShock.Serialization.Gateway.ShockerCommand> controlCommands)
        => QueueMessage(new GatewayToHubMessage
        {
            Payload = new GatewayToHubMessagePayload(new ShockerCommandList
            {
                Commands = [.. controlCommands.Select(x => new ShockerCommand()
                {
                    Duration = x.Duration,
                    Type = x.Type,
                    Id = x.Model == Serialization.Types.ShockerModelType.Petrainer998DR ? (ushort)(x.Id >> 1) : x.Id, // Fix for old hubs, their ids was serialized wrongly in the RFTransmitter, the V1 endpoint is being phased out, so this wont stay here forever
                    Intensity = x.Intensity,
                    Model = x.Model
                })]
            })
        });

    /// <inheritdoc />
    public override ValueTask<bool> EmergencyStop()
    {
        return ValueTask.FromResult(false);
    }

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
    public override ValueTask<bool> Reboot()
    {
        return ValueTask.FromResult(false);
    }


    /// <inheritdoc />
    public override ValueTask OtaInstall(SemVersion version)
        => QueueMessage(new GatewayToHubMessage
        {
            Payload = new GatewayToHubMessagePayload(new OtaInstall
            {
                Version = version.ToFbs()
            })
        });


}