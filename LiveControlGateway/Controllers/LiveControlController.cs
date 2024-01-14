using System.Net;
using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using OpenShock.Common.JsonSerialization;
using OpenShock.Common.Models;
using OpenShock.Common.Models.WebSocket;
using OpenShock.Common.Models.WebSocket.LCG;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;
using OpenShock.LiveControlGateway.LifetimeManager;
using OpenShock.LiveControlGateway.Websocket;
using OpenShock.ServicesCommon.Authentication;
using OpenShock.ServicesCommon.Models;
using OpenShock.ServicesCommon.Utils;
using OpenShock.ServicesCommon.Websocket;
using Timer = System.Timers.Timer;

namespace OpenShock.LiveControlGateway.Controllers;

[ApiController]
[Route("/{version:apiVersion}/ws/live/{deviceId:guid}")]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemas.SessionTokenCombo)]
public sealed class LiveControlController : WebsocketBaseController<IBaseResponse<LiveResponseType>>
{
    private readonly OpenShockContext _db;
    
    private static readonly TimeSpan PingInterval = TimeSpan.FromSeconds(5);
    private static readonly SharePermsAndLimitsLive OwnerPermsAndLimitsLive = new()
    {
        Shock = true,
        Vibrate = true,
        Sound = true,
        Duration = null,
        Intensity = null,
        Live = true
    };

    private LinkUser _currentUser;
    private Guid? _deviceId;
    private Device? _device;
    private Dictionary<Guid, SharePermsAndLimitsLive> _sharedShockers;

    /// <summary>
    /// Last latency in milliseconds, 0 initially
    /// </summary>
    public long LastLatency { get; private set; } = 0;

    private readonly Timer _pingTimer = new(PingInterval);

    public LiveControlController(OpenShockContext db,
        ILogger<LiveControlController> logger, IHostApplicationLifetime lifetime) : base(logger, lifetime)
    {
        _db = db;

        _pingTimer.Elapsed += (_, _) => LucTask.Run(SendPing);
    }

    /// <inheritdoc />
    protected override Task RegisterConnection()
    {
        WebsocketManager.LiveControlUsers.RegisterConnection(this);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task UnregisterConnection()
    {
        WebsocketManager.LiveControlUsers.UnregisterConnection(this);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Update all shockers permissions for this user on this device
    /// </summary>
    /// <param name="db"></param>
    [NonAction]
    public async Task UpdatePermissions(OpenShockContext db)
    {
        if (_device!.Owner == _currentUser.DbUser.Id)
        {
            Logger.LogTrace("User is owner, skipping update permissions");
            return;
        }
        
        Logger.LogDebug("Updating shared permissions for device [{Device}] for user [{User}]", Id, _currentUser.DbUser.Id);
        
        var updated = await db.ShockerShares
            .Where(x => x.Shocker.Device == Id && x.SharedWith == _currentUser.DbUser.Id).ToDictionaryAsync(
                x => x.ShockerId, x => new SharePermsAndLimitsLive
                {
                    Shock = x.PermShock,
                    Vibrate = x.PermVibrate,
                    Sound = x.PermSound,
                    Duration = x.LimitDuration,
                    Intensity = x.LimitIntensity,
                    Live = x.PermLive
                });
        
        _sharedShockers = updated;
    }

    /// <summary>
    /// We get the id from the route, check if its valid, check if the user has access to the shocker / device
    /// </summary>
    /// <returns></returns>
    protected override async Task<bool> ConnectionPrecondition()
    {
        if (HttpContext.GetRouteValue("deviceId") is not string param || !Guid.TryParse(param, out var id))
        {
            HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            return false;
        }

        _deviceId = id;

        var deviceExistsAndYouHaveAccess = await _db.Devices.AnyAsync(x =>
            x.Id == _deviceId && (x.Owner == _currentUser.DbUser.Id || x.Shockers.Any(y => y.ShockerShares.Any(
                z => z.SharedWith == _currentUser.DbUser.Id && z.PermLive))));

        if (deviceExistsAndYouHaveAccess)
        {
            _device = await _db.Devices.FirstOrDefaultAsync(x => x.Id == _deviceId);

            await UpdatePermissions(_db);

            return true;
        }

        HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
        await HttpContext.Response.WriteAsJsonAsync(new Common.Models.BaseResponse<object>
        {
            Message = "Device does not exist or you do not have access to it"
        });
        return false;
    }

    /// <summary>
    /// Authentication context
    /// </summary>
    /// <param name="context"></param>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        _currentUser = ControllerContext.HttpContext.RequestServices.GetRequiredService<IClientAuthService<LinkUser>>()
            .CurrentClient;
        base.OnActionExecuting(context);
    }

    /// <inheritdoc />
    public override Guid Id => _deviceId ?? throw new Exception("Device id is null");

    /// <inheritdoc />
    protected override async Task SendInitialData()
    {
        await UpdateConnectedState(DeviceLifetimeManager.IsConnected(Id), true);
    }

    private bool _lastIsConnected;

    /// <summary>
    /// Update the connected state of the device if different from what was last sent
    /// </summary>
    /// <param name="isConnected"></param>
    /// <param name="force"></param>
    [NonAction]
    public async Task UpdateConnectedState(bool isConnected, bool force = false)
    {
        if (_lastIsConnected == isConnected && !force) return;

        Logger.LogTrace("Sending connection update for device [{Device}] [{State}]", Id, isConnected);

        _lastIsConnected = isConnected;
        try
        {
            await QueueMessage(new Common.Models.WebSocket.BaseResponse<LiveResponseType>
            {
                ResponseType = _lastIsConnected
                    ? LiveResponseType.DeviceConnected
                    : LiveResponseType.DeviceNotConnected,
            });
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, "Error while sending device connected state");
        }
    }

    /// <inheritdoc />
    protected override async Task Logic()
    {
        Logger.LogDebug("Starting ping timer...");
        _pingTimer.Start();

        while (!Close.IsCancellationRequested)
        {
            try
            {
                if (WebSocket!.State == WebSocketState.Aborted) return;
                var message =
                    await JsonWebSocketUtils.ReceiveFullMessageAsyncNonAlloc<BaseRequest<LiveRequestType>>(WebSocket,
                        Linked.Token);

                if (message.IsT2)
                {
                    if (WebSocket.State != WebSocketState.Open)
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

                message.Switch(wsRequest =>
                    {
                        if (wsRequest?.Data == null) return;
                        LucTask.Run(() => ProcessResult(wsRequest));
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

    private Task ProcessResult(BaseRequest<LiveRequestType> request)
        => request.RequestType switch
        {
            LiveRequestType.Pong => IntakePong(request.Data),
            LiveRequestType.Frame => IntakeFrame(request.Data),
            _ => QueueMessage(new Common.Models.WebSocket.BaseResponse<LiveResponseType>()
            {
                ResponseType = LiveResponseType.RequestTypeNotFound
            }).AsTask()
        };

    /// <summary>
    /// Pong callback from the client, we can calculate latency from this
    /// </summary>
    /// <param name="requestData"></param>
    private async Task IntakePong(JsonDocument? requestData)
    {
        Logger.LogTrace("Intake pong");

        var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        PingResponse? pong;
        try
        {
            pong = requestData.NewSlDeserialize<PingResponse>();

            if (pong == null)
            {
                Logger.LogWarning("Error while deserializing pong");
                await QueueMessage(new Common.Models.WebSocket.BaseResponse<LiveResponseType>
                {
                    ResponseType = LiveResponseType.InvalidData
                });
                return;
            }
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, "Error while deserializing frame");
            await QueueMessage(new Common.Models.WebSocket.BaseResponse<LiveResponseType>
            {
                ResponseType = LiveResponseType.InvalidData
            });
            return;
        }

        var latency = currentTimestamp - pong.Timestamp;
        LastLatency = Math.Max(0, latency);

        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Latency: {Latency}ms (raw: {RawLatency}ms)", LastLatency, latency);

        await QueueMessage(new Common.Models.WebSocket.BaseResponse<LiveResponseType>
        {
            ResponseType = LiveResponseType.LatencyAnnounce,
            Data = new Dictionary<Guid, long>
            {
                { _currentUser.DbUser.Id, LastLatency }
            }
        });
    }


    private async Task IntakeFrame(JsonDocument? requestData)
    {
        Logger.LogTrace("Intake frame");
        ClientLiveFrame? frame;
        try
        {
            frame = requestData.NewSlDeserialize<ClientLiveFrame>();

            if (frame == null)
            {
                Logger.LogWarning("Error while deserializing frame");
                await QueueMessage(new Common.Models.WebSocket.BaseResponse<LiveResponseType>
                {
                    ResponseType = LiveResponseType.InvalidData
                });
                return;
            }
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, "Error while deserializing frame");
            await QueueMessage(new Common.Models.WebSocket.BaseResponse<LiveResponseType>
            {
                ResponseType = LiveResponseType.InvalidData
            });
            return;
        }
        
        Logger.LogTrace("Frame: {@Frame}", frame);

        var permCheck = CheckFramePermissions(frame.Shocker, frame.Type);
        if (permCheck.IsT1)
        {
            await QueueMessage(new Common.Models.WebSocket.BaseResponse<LiveResponseType>
            {
                ResponseType = LiveResponseType.ShockerNotFound
            });
            return;
        }

        if (permCheck.IsT2)
        {
            await QueueMessage(new Common.Models.WebSocket.BaseResponse<LiveResponseType>
            {
                ResponseType = LiveResponseType.ShockerMissingLivePermission
            });
            return;
        }

        if (permCheck.IsT3)
        {
            await QueueMessage(new Common.Models.WebSocket.BaseResponse<LiveResponseType>
            {
                ResponseType = LiveResponseType.ShockerMissingPermission
            });
            return;
        }
        
        
        var perms = permCheck.AsT0.Value;
        // Clamp to limits
        var intensityMax = perms.Intensity ?? 100;
        var intensity = Math.Clamp(frame.Intensity, (byte)0, intensityMax);

        var result = DeviceLifetimeManager.ReceiveFrame(Id, frame.Shocker, frame.Type, intensity);
        if (result.IsT0)
        {
            Logger.LogTrace("Successfully received frame");
        }

        if (result.IsT1)
        {
            await QueueMessage(new Common.Models.WebSocket.BaseResponse<LiveResponseType>
            {
                ResponseType = LiveResponseType.DeviceNotConnected
            });
            return;
        }

        if (result.IsT2)
        {
            await QueueMessage(new Common.Models.WebSocket.BaseResponse<LiveResponseType>
            {
                ResponseType = LiveResponseType.ShockerNotFound
            });
            return;
        }
    }

    private OneOf<Success<SharePermsAndLimitsLive>, NotFound, LiveNotEnabled, NoPermission> CheckFramePermissions(Guid shocker, ControlType controlType)
    {
        if (_device!.Owner == _currentUser.DbUser.Id)
            return new Success<SharePermsAndLimitsLive>(OwnerPermsAndLimitsLive);
        
        if (!_sharedShockers.TryGetValue(shocker, out var shockerShare) || !shockerShare.Live) return new NotFound();

        if (!IsAllowed(controlType, shockerShare)) return new NoPermission();

        return new Success<SharePermsAndLimitsLive>(shockerShare);
    }

    private static bool IsAllowed(ControlType type, SharePermsAndLimits? perms)
    {
        if (perms == null) return true;
        return type switch
        {
            ControlType.Shock => perms.Shock,
            ControlType.Vibrate => perms.Vibrate,
            ControlType.Sound => perms.Sound,
            ControlType.Stop => perms.Shock || perms.Vibrate || perms.Sound,
            _ => false
        };
    }


    /// <summary>
    /// Send a ping to the client, the client should respond with a pong
    /// </summary>
    private async Task SendPing()
    {
        if (WebSocket is not { State: WebSocketState.Open }) return;

        if (Logger.IsEnabled(LogLevel.Debug))
            Logger.LogDebug("Sending ping to live control user [{User}] for device [{Device}]", _currentUser.DbUser.Id,
                Id);

        await QueueMessage(new Common.Models.WebSocket.BaseResponse<LiveResponseType>
        {
            ResponseType = LiveResponseType.Ping,
            Data = new PingResponse
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }
        });
    }
    
    /// <inheritdoc />
    public override ValueTask DisposeControllerAsync()
    {
        Logger.LogTrace("Disposing controller timer");
        _pingTimer.Dispose();
        return base.DisposeControllerAsync();
    }
}

public struct LiveNotEnabled;
public struct NoPermission;