using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Authentication;
using OpenShock.Common.Hubs;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.Ota;
using OpenShock.Common.Services.RedisPubSub;
using OpenShock.Common.Utils;
using OpenShock.Serialization.Deprecated.DoNotUse.V1;
using OpenShock.Serialization.Types;
using Redis.OM.Contracts;
using Semver;
using Serilog;

namespace OpenShock.LiveControlGateway.Controllers;

/// <summary>
/// Communication with the devices aka ESP-32 micro controllers
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemas.DeviceToken)]
[ApiVersion("1")]
[Route("/{version:apiVersion}/ws/device")]
public sealed class DeviceV1Controller : DeviceControllerBase<HubToGatewayMessage, GatewayToHubMessage>
{
    private readonly IHubContext<UserHub, IUserHub> _userHubContext;

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
    /// <param name="redisPubService"></param>
    public DeviceV1Controller(
        ILogger<DeviceV1Controller> logger,
        IHostApplicationLifetime lifetime,
        IRedisConnectionProvider redisConnectionProvider,
        IDbContextFactory<OpenShockContext> dbContextFactory,
        IHubContext<UserHub, IUserHub> userHubContext,
        IServiceProvider serviceProvider, LCGConfig lcgConfig, IRedisPubService redisPubService)
        : base(logger, lifetime, HubToGatewayMessage.Serializer, GatewayToHubMessage.Serializer, redisConnectionProvider, 
            dbContextFactory, serviceProvider, lcgConfig, redisPubService)
    {
        _userHubContext = userHubContext;
    }
    
    private OtaUpdateStatus? _lastStatus;
    
    private IUserHub HcOwner => _userHubContext.Clients.User(CurrentDevice.Owner.ToString());
    
    /// <inheritdoc />
    protected override async Task Handle(HubToGatewayMessage data)
    {
        if(data.Payload == null) return;
        var payload = data.Payload.Value;

        await using var scope = ServiceProvider.CreateAsyncScope(); 
        var otaService = scope.ServiceProvider.GetRequiredService<IOtaService>();

        Logger.LogTrace("Received payload [{Kind}] from device [{DeviceId}]", payload.Kind, CurrentDevice.Id);
        switch (payload.Kind)
        {
            case HubToGatewayMessagePayload.ItemKind.KeepAlive:
                await SelfOnline(TimeSpan.FromMilliseconds(payload.KeepAlive.Uptime));
                break;

            case HubToGatewayMessagePayload.ItemKind.OtaInstallStarted:
                _lastStatus = OtaUpdateStatus.Started;
                await HcOwner.OtaInstallStarted(
                    CurrentDevice.Id,
                    payload.OtaInstallStarted.UpdateId,
                    payload.OtaInstallStarted.Version!.ToSemVersion());
                await otaService.Started(
                    CurrentDevice.Id,

                    payload.OtaInstallStarted.UpdateId,
                    payload.OtaInstallStarted.Version!.ToSemVersion());
                break;

            case HubToGatewayMessagePayload.ItemKind.OtaInstallProgress:
                await HcOwner.OtaInstallProgress(
                    CurrentDevice.Id,
                    payload.OtaInstallProgress.UpdateId,
                    payload.OtaInstallProgress.Task,
                    payload.OtaInstallProgress.Progress);

                if (_lastStatus == OtaUpdateStatus.Started)
                {
                    _lastStatus = OtaUpdateStatus.Running;
                    await otaService.Progress(CurrentDevice.Id, payload.OtaInstallProgress.UpdateId);
                }

                break;

            case HubToGatewayMessagePayload.ItemKind.OtaInstallFailed:
                await HcOwner.OtaInstallFailed(
                    CurrentDevice.Id,
                    payload.OtaInstallFailed.UpdateId,
                    payload.OtaInstallFailed.Fatal,
                    payload.OtaInstallFailed.Message!);

                await otaService.Error(CurrentDevice.Id, payload.OtaInstallFailed.UpdateId,
                    payload.OtaInstallFailed.Fatal, payload.OtaInstallFailed.Message!);

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
    public override ValueTask Control(List<OpenShock.Serialization.Gateway.ShockerCommand> controlCommands)
        => QueueMessage(new GatewayToHubMessage
        {
            Payload = new GatewayToHubMessagePayload(new ShockerCommandList
            {
                Commands = controlCommands.Select(x => new ShockerCommand()
                {
                    Duration = x.Duration,
                    Type = x.Type,
                    Id = x.Id,
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