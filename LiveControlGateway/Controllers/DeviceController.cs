using System.Globalization;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.Common.Utils;
using OpenShock.LiveControlGateway.LifetimeManager;
using OpenShock.LiveControlGateway.Websocket;
using OpenShock.Serialization;
using OpenShock.Serialization.Gateway;
using OpenShock.Serialization.Types;
using OpenShock.ServicesCommon.Authentication;
using OpenShock.ServicesCommon.Hubs;
using OpenShock.ServicesCommon.Services.Ota;
using Redis.OM.Contracts;
using Semver;
using Timer = System.Timers.Timer;

namespace OpenShock.LiveControlGateway.Controllers;

/// <summary>
/// Communication with the devices aka ESP-32 micro controllers
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemas.DeviceToken)]
[Route("/{version:apiVersion}/ws/device")]
public sealed class DeviceController : FlatbuffersWebsocketBaseController<GatewayToDeviceMessage>
{
    private Device _currentDevice = null!;
    private readonly IRedisConnectionProvider _redisConnectionProvider;
    private readonly IDbContextFactory<OpenShockContext> _dbContextFactory;
    private readonly IHubContext<UserHub, IUserHub> _userHubContext;
    private readonly IServiceProvider _serviceProvider;
    private static readonly TimeSpan InitialTimeout = TimeSpan.FromSeconds(65);
    private static readonly TimeSpan KeepAliveTimeout = TimeSpan.FromSeconds(35);
    private static readonly object KeepAliveTimeoutInt = (int)KeepAliveTimeout.TotalSeconds;
    private readonly Timer _keepAliveTimer = new(InitialTimeout);

    /// <summary>
    /// Authentication context
    /// </summary>
    /// <param name="context"></param>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        _currentDevice = ControllerContext.HttpContext.RequestServices
            .GetRequiredService<IClientAuthService<Device>>()
            .CurrentClient;
        base.OnActionExecuting(context);
    }

    /// <inheritdoc />
    public override Guid Id => _currentDevice.Id;

    /// <summary>
    /// DI
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="lifetime"></param>
    /// <param name="redisConnectionProvider"></param>
    /// <param name="dbContextFactory"></param>
    /// <param name="userHubContext"></param>
    /// <param name="serviceProvider"></param>
    public DeviceController(
        ILogger<DeviceController> logger,
        IHostApplicationLifetime lifetime,
        IRedisConnectionProvider redisConnectionProvider,
        IDbContextFactory<OpenShockContext> dbContextFactory,
        IHubContext<UserHub, IUserHub> userHubContext,
        IServiceProvider serviceProvider)
        : base(logger, lifetime, GatewayToDeviceMessage.Serializer)
    {
        _redisConnectionProvider = redisConnectionProvider;
        _dbContextFactory = dbContextFactory;
        _userHubContext = userHubContext;
        _serviceProvider = serviceProvider;
        _keepAliveTimer.Elapsed += async (sender, args) =>
        {
            Logger.LogWarning("Keep alive timeout reached, closing websocket connection");
            await Close.CancelAsync();
        };
        _keepAliveTimer.Start();
    }

    /// <inheritdoc />
    protected override async Task Logic()
    {
        while (true)
        {
            try
            {
                if (WebSocket?.State == WebSocketState.Aborted) return;
                var message =
                    await FlatbufferWebSocketUtils.ReceiveFullMessageAsyncNonAlloc(WebSocket!,
                        DeviceToGatewayMessage.Serializer,
                        Linked.Token);

                if (message.IsT2)
                {
                    if (WebSocket!.State != WebSocketState.Open)
                    {
                        Logger.LogWarning("Client sent closure, but connection state is not open");
                        break;
                    }

                    try
                    {
                        await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal close",
                            Linked.Token);
                    }
                    catch (OperationCanceledException e)
                    {
                        Logger.LogError(e, "Error during close handshake");
                    }

                    Logger.LogInformation("Closing websocket connection");
                    break;
                }

                message.Switch(serverMessage =>
                    {
                        if (serverMessage?.Payload == null) return;
                        var payload = serverMessage.Payload.Value;
#pragma warning disable CS4014
                        LucTask.Run(() => Handle(payload));
#pragma warning restore CS4014
                    },
                    failed => { Logger.LogWarning(failed.Exception, "Deserialization failed for websocket message"); },
                    _ => { });
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation("WebSocket connection terminated due to close or shutdown");
                break;
            }
            catch (WebSocketException e)
            {
                if (e.WebSocketErrorCode != WebSocketError.ConnectionClosedPrematurely)
                    Logger.LogError(e, "Error in receive loop, websocket exception");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Exception while processing websocket request");
            }
        }

        await Close.CancelAsync();
    }

    private OtaUpdateStatus? _lastStatus;

    private async Task Handle(DeviceToGatewayMessagePayload payload)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var otaService = scope.ServiceProvider.GetRequiredService<IOtaService>();

        Logger.LogTrace("Received payload [{Kind}] from device [{DeviceId}]", payload.Kind, _currentDevice.Id);
        switch (payload.Kind)
        {
            case DeviceToGatewayMessagePayload.ItemKind.KeepAlive:
                await SelfOnline();
                break;

            case DeviceToGatewayMessagePayload.ItemKind.OtaInstallStarted:
                Logger.LogInformation("OTA started {Version}", payload.OtaInstallStarted.Version!.ToSemVersion());
                _lastStatus = OtaUpdateStatus.Started;
                await HcOwner.OtaInstallStarted(
                    _currentDevice.Id,
                    payload.OtaInstallStarted.UpdateId,
                    payload.OtaInstallStarted.Version!.ToSemVersion());
                await otaService.Started(
                    _currentDevice.Id,
                    payload.OtaInstallStarted.UpdateId,
                    payload.OtaInstallStarted.Version!.ToSemVersion());
                break;

            case DeviceToGatewayMessagePayload.ItemKind.OtaInstallProgress:
                Logger.LogInformation("OTA progress {Progress}", payload.OtaInstallProgress.Progress.ToString(CultureInfo.InvariantCulture));
                await HcOwner.OtaInstallProgress(
                    _currentDevice.Id,
                    payload.OtaInstallProgress.UpdateId,
                    payload.OtaInstallProgress.Task,
                    payload.OtaInstallProgress.Progress);

                if (_lastStatus == OtaUpdateStatus.Started)
                {
                    _lastStatus = OtaUpdateStatus.Running;
                    await otaService.Progress(_currentDevice.Id, payload.OtaInstallProgress.UpdateId);
                }

                break;

            case DeviceToGatewayMessagePayload.ItemKind.OtaInstallFailed:
                await HcOwner.OtaInstallFailed(
                    _currentDevice.Id,
                    payload.OtaInstallFailed.UpdateId,
                    payload.OtaInstallFailed.Fatal,
                    payload.OtaInstallFailed.Message!);

                _lastStatus = OtaUpdateStatus.Error;
                break;

            case DeviceToGatewayMessagePayload.ItemKind.BootStatus:
                Logger.LogInformation("Boot status {BootType}", payload.BootStatus.BootType);
                if (payload.BootStatus.BootType == FirmwareBootType.NewFirmware)
                {
                    await HcOwner.OtaInstallSucceeded(
                        _currentDevice.Id, payload.BootStatus.OtaUpdateId);

                    
                    var test = await otaService.Success(_currentDevice.Id, payload.BootStatus.OtaUpdateId);
                    Logger.LogInformation("SUCCESS DB UPDATE {A}", test);
                    _lastStatus = OtaUpdateStatus.Finished;
                    break;
                }

                if (payload.BootStatus.BootType == FirmwareBootType.Rollback)
                {
                    await HcOwner.OtaRollback(
                        _currentDevice.Id, payload.BootStatus.OtaUpdateId);

                    await otaService.Error(_currentDevice.Id, payload.BootStatus.OtaUpdateId);
                    _lastStatus = OtaUpdateStatus.Error;
                    break;
                }

                break;

            case DeviceToGatewayMessagePayload.ItemKind.NONE:
            default:
                Logger.LogWarning("Payload kind not defined [{Kind}]", payload.Kind);
                break;
        }
    }

    private IUserHub HcOwner => _userHubContext.Clients.User(_currentDevice.Owner.ToString());

    private async Task SelfOnline()
    {
        Logger.LogDebug("Received keep alive from device [{DeviceId}]", _currentDevice.Id);

        _keepAliveTimer.Interval = KeepAliveTimeout.TotalMilliseconds;

        var deviceOnline = _redisConnectionProvider.RedisCollection<DeviceOnline>();
        var deviceId = _currentDevice.Id.ToString();
        var online = await deviceOnline.FindByIdAsync(deviceId);
        if (online == null)
        {
            await deviceOnline.InsertAsync(new DeviceOnline
            {
                Id = _currentDevice.Id,
                Owner = _currentDevice.Owner,
                FirmwareVersion = FirmwareVersion,
                Gateway = LCGGlobals.LCGConfig.Fqdn
            }, KeepAliveTimeout);
            return;
        }

        if (online.FirmwareVersion != FirmwareVersion || online.Gateway != LCGGlobals.LCGConfig.Fqdn)
        {
            online.Gateway = LCGGlobals.LCGConfig.Fqdn;
            online.FirmwareVersion = FirmwareVersion;
            await deviceOnline.SaveAsync();
            Logger.LogInformation("Updated details of online device");
        }

        await _redisConnectionProvider.Connection.ExecuteAsync("EXPIRE",
            $"{typeof(DeviceOnline).FullName}:{_currentDevice.Id}", KeepAliveTimeoutInt);
    }

    private SemVersion? FirmwareVersion { get; set; }

    /// <inheritdoc />
    protected override async Task RegisterConnection()
    {
        if (HttpContext.Request.Headers.TryGetValue("Firmware-Version", out var header) &&
            SemVersion.TryParse(header, SemVersionStyles.Strict, out var version)) FirmwareVersion = version;

        await using var db = await _dbContextFactory.CreateDbContextAsync();
        await DeviceLifetimeManager.AddDeviceConnection(this, db, _dbContextFactory, Linked.Token);
    }

    /// <inheritdoc />
    protected override async Task UnregisterConnection()
    {
        Logger.LogDebug("Unregistering device connection [{DeviceId}]", Id);
        await DeviceLifetimeManager.RemoveDeviceConnection(this);
    }

    /// <inheritdoc />
    public override ValueTask DisposeControllerAsync()
    {
        Logger.LogTrace("Disposing controller timer");
        _keepAliveTimer.Dispose();
        return base.DisposeControllerAsync();
    }
}