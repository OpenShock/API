using System.Diagnostics;
using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using OpenShock.Common.Authentication;
using OpenShock.Common.Authentication.Attributes;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.Constants;
using OpenShock.Common.Errors;
using OpenShock.Common.JsonSerialization;
using OpenShock.Common.Models;
using OpenShock.Common.Models.WebSocket;
using OpenShock.Common.Models.WebSocket.LCG;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;
using OpenShock.Common.Utils;
using OpenShock.Common.Websocket;
using OpenShock.LiveControlGateway.LifetimeManager;
using OpenShock.LiveControlGateway.Models;
using OpenShock.LiveControlGateway.Websocket;
using Timer = System.Timers.Timer;

namespace OpenShock.LiveControlGateway.Controllers;

/// <summary>
/// Live control controller
/// </summary>
[ApiController]
[Route("/{version:apiVersion}/ws/live/{hubId:guid}")]
[TokenPermission(PermissionType.Shockers_Use)]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemas.UserSessionApiTokenCombo)]
public sealed class LiveControlController : WebsocketBaseController<IBaseResponse<LiveResponseType>>, IActionFilter
{
    private readonly OpenShockContext _db;
    private readonly HubLifetimeManager _hubLifetimeManager;

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

    private User _currentUser = null!;
    private Guid? _hubId;
    private Device? _device;
    private Dictionary<Guid, LiveShockerPermission> _sharedShockers = new();
    private byte _tps = 10;
    private long _pingTimestamp = Stopwatch.GetTimestamp();
    private ushort _latencyMs = 0;
    
    /// <summary>
    /// Connection Id for this connection, unique and random per connection
    /// </summary>
    public Guid ConnectionId => Guid.NewGuid();

    private readonly Timer _pingTimer = new(PingInterval);

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="db"></param>
    /// <param name="logger"></param>
    /// <param name="lifetime"></param>
    /// <param name="hubLifetimeManager"></param>
    public LiveControlController(
        OpenShockContext db,
        ILogger<LiveControlController> logger,
        IHostApplicationLifetime lifetime,
        HubLifetimeManager hubLifetimeManager) : base(logger, lifetime)
    {
        _db = db;
        _hubLifetimeManager = hubLifetimeManager;
        _pingTimer.Elapsed += (_, _) => LucTask.Run(SendPing);
    }

    /// <inheritdoc />
    protected override Task<bool> TryRegisterConnection()
    {
        WebsocketManager.LiveControlUsers.RegisterConnection(this);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    protected override Task UnregisterConnection()
    {
        WebsocketManager.LiveControlUsers.UnregisterConnection(this);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Update all shockers permissions for this user on this hub
    /// </summary>
    /// <param name="db"></param>
    [NonAction]
    public async Task UpdatePermissions(OpenShockContext db)
    {
        Logger.LogDebug("Updating shared permissions for hub [{HubId}] for user [{User}]", Id,
            _currentUser.Id);
        
        if (_device!.Owner == _currentUser.Id)
        {
            Logger.LogTrace("User is owner of hub");
            _sharedShockers = await db.Shockers.Where(x => x.Device == Id).ToDictionaryAsync(x => x.Id, x => new LiveShockerPermission()
            {
                Paused = x.Paused,
                PermsAndLimits = OwnerPermsAndLimitsLive
            });
            return;
        }
        
        _sharedShockers = await db.ShockerShares
            .Where(x => x.Shocker.Device == Id && x.SharedWith == _currentUser.Id).Select(x => new
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
    /// We get the id from the route, check if its valid, check if the user has access to the shocker / hub
    /// </summary>
    /// <returns></returns>
    protected override async Task<OneOf<Success, OneOf.Types.Error<OpenShockProblem>>> ConnectionPrecondition()
    {
        if (HttpContext.GetRouteValue("hubId") is not string param || !Guid.TryParse(param, out var id))
        {
            return new OneOf.Types.Error<OpenShockProblem>(WebsocketError.WebsocketLiveControlHubIdInvalid);
        }

        _hubId = id;

        var hubExistsAndYouHaveAccess = await _db.Devices.AnyAsync(x =>
            x.Id == _hubId && (x.Owner == _currentUser.Id || x.Shockers.Any(y => y.ShockerShares.Any(
                z => z.SharedWith == _currentUser.Id && z.PermLive))));

        if (!hubExistsAndYouHaveAccess)
        {
            return new OneOf.Types.Error<OpenShockProblem>(WebsocketError.WebsocketLiveControlHubNotFound);
        }
        
        _device = await _db.Devices.FirstOrDefaultAsync(x => x.Id == _hubId);

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

        return new Success();
    }

    /// <summary>
    /// Authentication context
    /// </summary>
    /// <param name="context"></param>
    [NonAction]
    public void OnActionExecuting(ActionExecutingContext context)
    {
        _currentUser = ControllerContext.HttpContext.RequestServices.GetRequiredService<IClientAuthService<User>>()
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
    public override Guid Id => _hubId ?? throw new Exception("Hub id is null");

    /// <inheritdoc />
    protected override async Task SendInitialData()
    {
        await QueueMessage(new Common.Models.WebSocket.BaseResponse<LiveResponseType>
        {
            ResponseType = LiveResponseType.TPS,
            Data = new TpsData
            {
                Client = _tps
            }
        });
        await UpdateConnectedState(_hubLifetimeManager.IsConnected(Id), true);
    }

    private bool _lastIsConnected;

    /// <summary>
    /// Update the connected state of the hub if different from what was last sent
    /// </summary>
    /// <param name="isConnected"></param>
    /// <param name="force"></param>
    [NonAction]
    public async Task UpdateConnectedState(bool isConnected, bool force = false)
    {
        if (_lastIsConnected == isConnected && !force) return;

        Logger.LogTrace("Sending connection update for hub [{HubId}] [{State}]", Id, isConnected);

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
            Logger.LogWarning(e, "Error while sending hub connected state");
        }
    }

    /// <inheritdoc />
    protected override async Task Logic()
    {
        Logger.LogDebug("Starting ping timer...");
        _pingTimer.Start();

        while (!LinkedToken.IsCancellationRequested)
        {
            try
            {
                if (WebSocket!.State == WebSocketState.Aborted) break;
                var message =
                    await JsonWebSocketUtils.ReceiveFullMessageAsyncNonAlloc<BaseRequest<LiveRequestType>>(WebSocket,
                        LinkedToken);

                if (message.IsT2)
                {
                    if (WebSocket.State != WebSocketState.Open)
                    {
                        Logger.LogTrace("Client sent closure, but connection state is not open");
                        break;
                    }

                    try
                    {
                        await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal close",
                            LinkedToken);
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
            LiveRequestType.BulkFrame => IntakeBulkFrame(request.Data),
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
        
        // Received pong without sending ping, this could be abusing the pong endpoint.
        if (_pingTimestamp == 0)
        {
            // TODO: Kick or warn client.
            return;
        }

        _latencyMs = (ushort)Math.Min(Stopwatch.GetElapsedTime(_pingTimestamp).TotalMilliseconds, ushort.MaxValue); // If someone has a ping higher than 65 seconds, they are messing with us. Cap it to 65 seconds
        _pingTimestamp = 0;

        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Latency: {Latency}ms", _latencyMs);

        await QueueMessage(new Common.Models.WebSocket.BaseResponse<LiveResponseType>
        {
            ResponseType = LiveResponseType.LatencyAnnounce,
            Data = new LatencyAnnounceData
            {
                DeviceLatency = 0, // TODO: Implement hub latency calculation
                OwnLatency = _latencyMs
            }
        });
    }

    /// <summary>
    /// Intake bulk frame
    /// </summary>
    /// <param name="requestData"></param>
    /// <returns></returns>
    private async Task IntakeBulkFrame(JsonDocument? requestData)
    {
        ClientLiveFrame[]? frames;
        try
        {
            frames = requestData.NewSlDeserialize<ClientLiveFrame[]>();

            if (frames is not { Length: > 0 })
            {
                Logger.LogWarning("Error while deserializing bulk frame");
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

        var task = Task.WhenAll(frames.Select(ProcessFrameInternal));

        try
        {
            await task;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Exception while processing bulk frame. One or more intake frame tasks failed");
        }
    }

    /// <summary>
    /// Intake frame
    /// </summary>
    /// <param name="requestData"></param>
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

        await ProcessFrameInternal(frame);
    }

    private async Task ProcessFrameInternal(ClientLiveFrame frame)
    {
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
        var intensity = Math.Clamp(frame.Intensity, HardLimits.MinControlIntensity, perms.Intensity ?? HardLimits.MaxControlIntensity);

        var result = _hubLifetimeManager.ReceiveFrame(Id, frame.Shocker, frame.Type, intensity, _tps);
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
            Logger.LogDebug("Sending ping to live control user [{UserId}] for hub [{HubId}]", _currentUser.Id,
                Id);

        _pingTimestamp = Stopwatch.GetTimestamp();
        await QueueMessage(new Common.Models.WebSocket.BaseResponse<LiveResponseType>
        {
            ResponseType = LiveResponseType.Ping,
            Data = new LcgLiveControlPing
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

/// <summary>
/// OneOf
/// </summary>
public readonly struct LiveNotEnabled;

/// <summary>
/// OneOf
/// </summary>
public readonly struct NoPermission;

/// <summary>
/// OneOf
/// </summary>
public readonly struct ShockerPaused;