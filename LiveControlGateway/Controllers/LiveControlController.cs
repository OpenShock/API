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
using OpenShock.LiveControlGateway.Models;
using OpenShock.LiveControlGateway.Websocket;
using OpenShock.ServicesCommon.Authentication;
using OpenShock.ServicesCommon.Authentication.Attributes;
using OpenShock.ServicesCommon.Authentication.Services;
using OpenShock.ServicesCommon.Models;
using OpenShock.ServicesCommon.Utils;
using OpenShock.ServicesCommon.Websocket;
using Timer = System.Timers.Timer;

namespace OpenShock.LiveControlGateway.Controllers;

[ApiController]
[Route("/{version:apiVersion}/ws/live/{deviceId:guid}")]
[TokenPermission(PermissionType.Shockers_Use)]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemas.SessionTokenCombo)]
public sealed class LiveControlController : WebsocketBaseController<IBaseResponse<LiveResponseType>>, IActionFilter
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
    private Dictionary<Guid, LiveShockerPermission> _sharedShockers;
    private byte _tps = 10;
    
    /// <summary>
    /// Connection Id for this connection, unique and random per connection
    /// </summary>
    public Guid ConnectionId => Guid.NewGuid();

    /// <summary>
    /// Last latency in milliseconds, 0 initially
    /// </summary>
    public ulong LastLatency { get; private set; } = 0;

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
        Logger.LogDebug("Updating shared permissions for device [{Device}] for user [{User}]", Id,
            _currentUser.DbUser.Id);
        
        if (_device!.Owner == _currentUser.DbUser.Id)
        {
            Logger.LogTrace("User is owner of device");
            _sharedShockers = await db.Shockers.Where(x => x.Device == Id).ToDictionaryAsync(x => x.Id, x => new LiveShockerPermission()
            {
                Paused = x.Paused,
                PermsAndLimits = OwnerPermsAndLimitsLive
            });
            return;
        }
        
        _sharedShockers = await db.ShockerShares
            .Where(x => x.Shocker.Device == Id && x.SharedWith == _currentUser.DbUser.Id).Select(x => new
            {
                x.ShockerId,
                Lsp = new LiveShockerPermission
                {
                    Paused = x.Paused,
                    PermsAndLimits = new SharePermsAndLimitsLive
                    {
                        Shock = x.PermShock,
                        Vibrate = x.PermVibrate,
                        Sound = x.PermSound,
                        Duration = x.LimitDuration,
                        Intensity = x.LimitIntensity,
                        Live = x.PermLive
                    }
                }
            }).ToDictionaryAsync(x => x.ShockerId, x => x.Lsp);
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

        if (!deviceExistsAndYouHaveAccess)
        {
            HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            await HttpContext.Response.WriteAsJsonAsync(new Common.Models.BaseResponse<object>
            {
                Message = "Device does not exist or you do not have access to it"
            });
            return false;
        }
        
        _device = await _db.Devices.FirstOrDefaultAsync(x => x.Id == _deviceId);

        await UpdatePermissions(_db);
        
        if(HttpContext.Request.Query.TryGetValue("tps", out var requestedTps))
        {
            if (requestedTps.Count == 1)
            {
                var firstElement = requestedTps[0];
                if (byte.TryParse(firstElement, out var tps))
                {
                    _tps = Math.Clamp(tps, (byte)1, (byte)10);
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Authentication context
    /// </summary>
    /// <param name="context"></param>
    [NonAction]
    public void OnActionExecuting(ActionExecutingContext context)
    {
        _currentUser = ControllerContext.HttpContext.RequestServices.GetRequiredService<IClientAuthService<LinkUser>>()
            .CurrentClient;
    }

    /// <summary>
    /// Post action context
    /// </summary>
    /// <param name="context"></param>
    [NonAction]
    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    /// <inheritdoc />
    public override Guid Id => _deviceId ?? throw new Exception("Device id is null");

    /// <inheritdoc />
    protected override async Task SendInitialData()
    {
        await QueueMessage(new Common.Models.WebSocket.BaseResponse<LiveResponseType>
        {
            ResponseType = LiveResponseType.TPS,
            Data = new TpsData
            {
                Client = _tps,
                Server = 10
            }
        });
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

        while (!Linked.IsCancellationRequested)
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
        LastLatency = Convert.ToUInt64(Math.Max(0, latency));

        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Latency: {Latency}ms (raw: {RawLatency}ms)", LastLatency, latency);

        await QueueMessage(new Common.Models.WebSocket.BaseResponse<LiveResponseType>
        {
            ResponseType = LiveResponseType.LatencyAnnounce,
            Data = new LatencyAnnounceData
            {
                DeviceLatency = 0, // TODO: Implement device latency calculation
                OwnLatency = LastLatency
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

        if (permCheck.IsT4)
        {
            await QueueMessage(new Common.Models.WebSocket.BaseResponse<LiveResponseType>()
            {
                ResponseType = LiveResponseType.ShockerPaused
            });
            return;
        }


        var perms = permCheck.AsT0.Value;
        // Clamp to limits
        var intensityMax = perms.Intensity ?? 100;
        var intensity = Math.Clamp(frame.Intensity, (byte)0, intensityMax);

        var result = DeviceLifetimeManager.ReceiveFrame(Id, frame.Shocker, frame.Type, intensity, _tps);
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
        
        if (result.IsT3)
        {
            await QueueMessage(new Common.Models.WebSocket.BaseResponse<LiveResponseType>
            {
                ResponseType = LiveResponseType.ShockerExclusive,
                Data = result.AsT3.Until
            });
            return;
        }
    }

    private OneOf<Success<SharePermsAndLimitsLive>, NotFound, LiveNotEnabled, NoPermission, ShockerPaused> CheckFramePermissions(
        Guid shocker, ControlType controlType)
    {
        if (!_sharedShockers.TryGetValue(shocker, out var shockerShare)) return new NotFound();

        if (shockerShare.Paused) return new ShockerPaused();
        if (!IsAllowed(controlType, shockerShare.PermsAndLimits)) return new NoPermission();

        return new Success<SharePermsAndLimitsLive>(shockerShare.PermsAndLimits);
    }

    private static bool IsAllowed(ControlType type, SharePermsAndLimitsLive? perms)
    {
        if (perms == null) return true;
        if (!perms.Live) return false;
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
public struct ShockerPaused;