using System.Globalization;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.Common.Utils;
using OpenShock.LiveControlGateway.LifetimeManager;
using OpenShock.LiveControlGateway.Websocket;
using OpenShock.Serialization;
using OpenShock.ServicesCommon.Authentication;
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
public sealed class DeviceController : FlatbuffersWebsocketBaseController<ServerToDeviceMessage>
{
    private Device _currentDevice = null!;
    private readonly IRedisConnectionProvider _redisConnectionProvider;
    private readonly OpenShockContext _db;
    private readonly IDbContextFactory<OpenShockContext> _dbContextFactory;
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
    /// <param name="db"></param>
    /// <param name="dbContextFactory"></param>
    public DeviceController(ILogger<DeviceController> logger, IHostApplicationLifetime lifetime,
        IRedisConnectionProvider redisConnectionProvider, OpenShockContext db, IDbContextFactory<OpenShockContext> dbContextFactory)
        : base(logger, lifetime, ServerToDeviceMessage.Serializer)
    {
        _redisConnectionProvider = redisConnectionProvider;
        _db = db;
        _dbContextFactory = dbContextFactory;
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
                    await FlatbufferWebSocketUtils.ReceiveFullMessageAsyncNonAlloc(WebSocket!, DeviceToServerMessage.Serializer,
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

    private async Task Handle(DeviceToServerMessagePayload payload)
    {
        switch (payload.Kind)
        {
            case DeviceToServerMessagePayload.ItemKind.KeepAlive:
                await SelfOnline();
                break;
            case DeviceToServerMessagePayload.ItemKind.NONE:
            default:
                Logger.LogWarning("Payload kind not defined [{Kind}]", payload.Kind);
                break;
        }
    }

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

        if (online.FirmwareVersion != FirmwareVersion)
        {
            online.FirmwareVersion = FirmwareVersion;
            await deviceOnline.SaveAsync();
            Logger.LogInformation("Updated firmware version of online device");
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

        await DeviceLifetimeManager.AddDeviceConnection(this, _db, _dbContextFactory, Linked.Token);
         
        WebsocketManager.ServerToDevice.RegisterConnection(this);
    }

    /// <inheritdoc />
    protected override async Task UnregisterConnection()
    {
        Logger.LogDebug("Unregistering device connection [{DeviceId}]", Id);
        await DeviceLifetimeManager.RemoveDeviceConnection(this);
        WebsocketManager.ServerToDevice.UnregisterConnection(this);
    }

    public override ValueTask DisposeControllerAsync()
    {
        Logger.LogTrace("Disposing controller timer");
        _keepAliveTimer.Dispose();
        return base.DisposeControllerAsync();
    }
}