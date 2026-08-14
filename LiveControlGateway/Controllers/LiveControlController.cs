using System.Diagnostics;
using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Authentication;
using OpenShock.Common.Authentication.Attributes;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.Constants;
using OpenShock.Common.Errors;
using OpenShock.Common.Models;
using OpenShock.Common.Models.WebSocket;
using OpenShock.Common.Models.WebSocket.LCG;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;
using OpenShock.Common.Utils;
using OpenShock.Common.Websocket;
using OpenShock.LiveControlGateway.LifetimeManager;
using OpenShock.LiveControlGateway.Models;
using OpenShock.LiveControlGateway.PubSub;
using JsonOptions = OpenShock.Common.JsonSerialization.JsonOptions;
using Results = OpenShock.Common.Results;
using Timer = System.Timers.Timer;

using OpenShock.Internal.Common.Utils;

using OpenShock.Internal.Common.Constants;

using OpenShock.Internal.Common.Problems;

namespace OpenShock.LiveControlGateway.Controllers;

/// <summary>
/// Live control controller
/// </summary>
[ApiController]
[Route("/{version:apiVersion}/ws/live/{hubId:guid}")]
[TokenPermission(PermissionType.Shockers_Use)]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemes.UserSessionApiTokenCombo)]
public sealed class LiveControlController : WebsocketBaseController<LiveControlResponse<LiveResponseType>>,
    IActionFilter
{
    private readonly HubLifetimeManager _hubLifetimeManager;
    private readonly IDbContextFactory<OpenShockContext> _dbContextFactory;
    private readonly IUserReferenceService _userReferenceService;
    private readonly ApiTokenUpdateSubscriber _tokenUpdateSubscriber;
    private readonly ILogger<LiveControlController> _logger;

    private static readonly TimeSpan PingInterval = TimeSpan.FromSeconds(5);

    private static readonly SharePermsAndLimits OwnerPermsAndLimitsLive = new()
    {
        Shock = true,
        Vibrate = true,
        Sound = true,
        Duration = null,
        Intensity = null,
        Live = true
    };

    private User _currentUser = null!;

    /// <summary>
    /// Id of the API token used to authenticate, if any. Null when authenticated via a user session.
    /// </summary>
    private Guid? _tokenId;

    /// <summary>
    /// Shocker control scoping carried by the API token used to authenticate, if any.
    /// Null when authenticated via a user session (no token limits apply).
    /// </summary>
    private ApiTokenControlLimits? _tokenLimits;

    /// <summary>
    /// When true, the authenticating API token is paused (or was revoked) and may not send any control frames.
    /// </summary>
    private bool _tokenPaused;

    /// <summary>
    /// ID of the connected hub
    /// </summary>
    public Guid? HubId { get; private set; }

    private Device? _device;
    private Dictionary<Guid, LiveShockerPermission> _sharedShockers = new();
    private byte _tps = 10;
    private long _pingTimestamp = Stopwatch.GetTimestamp();
    private ushort _latencyMs;
    private HubLifetime? _hubLifetime;

    private HubLifetime HubLifetime =>
        _hubLifetime ?? throw new InvalidOperationException("Hub lifetime is null but was accessed");

    /// <summary>
    /// Connection Id for this connection, unique and random per connection
    /// </summary>
    public Guid ConnectionId => Guid.CreateVersion7();

    private readonly Timer _pingTimer = new(PingInterval);

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="dbContextFactory"></param>
    /// <param name="userReferenceService"></param>
    /// <param name="tokenUpdateSubscriber"></param>
    /// <param name="hubLifetimeManager"></param>
    public LiveControlController(HubLifetimeManager hubLifetimeManager, IDbContextFactory<OpenShockContext> dbContextFactory, IUserReferenceService userReferenceService, ApiTokenUpdateSubscriber tokenUpdateSubscriber, ILogger<LiveControlController> logger) : base(logger)
    {
        _hubLifetimeManager = hubLifetimeManager;
        _dbContextFactory = dbContextFactory;
        _userReferenceService = userReferenceService;
        _tokenUpdateSubscriber = tokenUpdateSubscriber;
        _logger = logger;
        
        _pingTimer.Elapsed += (_, _) => OsTask.Run(SendPing);
    }

    
    private bool _unregistered;

    /// <inheritdoc />
    protected override async Task UnregisterConnection()
    {
        if (Interlocked.Exchange(ref _unregistered, true))
            return;

        if (_hubLifetime is null) return;
        if (!await _hubLifetime.RemoveLiveControlClient(this))
        {
            _logger.LogError("Failed to remove live control client from hub lifetime {HubId} {CurrentUserId}", HubId,
                _currentUser.Id);
        }
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

        if (_device!.OwnerId == _currentUser.Id)
        {
            Logger.LogTrace("User is owner of hub");
            _sharedShockers = await db.Shockers.Where(x => x.DeviceId == Id).ToDictionaryAsync(x => x.Id, x =>
                new LiveShockerPermission()
                {
                    Paused = x.IsPaused,
                    PermsAndLimits = OwnerPermsAndLimitsLive
                });
            return;
        }

        _sharedShockers = await db.UserShares
            .Where(x => x.Shocker.DeviceId == Id && x.SharedWithUserId == _currentUser.Id).Select(x => new
            {
                x.ShockerId,
                Lsp = new LiveShockerPermission
                {
                    Paused = x.IsPaused,
                    PermsAndLimits = new SharePermsAndLimits
                    {
                        Sound = x.AllowSound,
                        Vibrate = x.AllowVibrate,
                        Shock = x.AllowShock,
                        Duration = x.MaxDuration,
                        Intensity = x.MaxIntensity,
                        Live = x.AllowLiveControl
                    }
                }
            }).ToDictionaryAsync(x => x.ShockerId, x => x.Lsp);
    }

    /// <summary>
    /// We get the id from the route, check if its valid, check if the user has access to the shocker / hub
    /// </summary>
    /// <returns></returns>
    protected override async Task<Results.SuccessOrProblem> ConnectionPrecondition()
    {
        if (HttpContext.GetRouteValue("hubId") is not string param || !Guid.TryParse(param, out var id))
        {
            return WebsocketError.WebsocketLiveControlHubIdInvalid;
        }

        HubId = id;

        await using (var db = await _dbContextFactory.CreateDbContextAsync())
        {
            var hubExistsAndYouHaveAccess = await db.Devices.AnyAsync(x =>
                x.Id == HubId && (x.OwnerId == _currentUser.Id || x.Shockers.Any(y =>
                    y.UserShares.Any(z => z.SharedWithUserId == _currentUser.Id && z.AllowLiveControl))));

            if (!hubExistsAndYouHaveAccess)
            {
                return WebsocketError.WebsocketLiveControlHubNotFound;
            }

            _device = await db.Devices.FirstOrDefaultAsync(x => x.Id == HubId);

            await UpdatePermissions(db);
        }

        if (HttpContext.Request.Query.TryGetValue("tps", out var requestedTps))
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

        var hubLifetimeResult = await _hubLifetimeManager.AddLiveControlConnection(this);

        switch (hubLifetimeResult)
        {
            case Results.NotFound:
                _logger.LogDebug("No such hub with id [{HubId}] connected", HubId);
                return WebsocketError.WebsocketLiveControlHubNotConnected;
            case LifetimeManager.HubLifetimeManager.Busy:
                _logger.LogDebug("Hub is busy, cannot connect [{HubId}]", HubId);
                return WebsocketError.WebsocketLiveControlHubLifetimeBusy;
            case LifetimeManager.HubLifetime hubLifetime:
                _hubLifetime = hubLifetime;
                break;
        }

        return new Results.Success();
    }

    /// <summary>
    /// Authentication context
    /// </summary>
    /// <param name="context"></param>
    [NonAction]
    public void OnActionExecuting(ActionExecutingContext context)
    {
        _currentUser = HttpContext.Items["User"] as User ?? throw new Exception("User not found");

        // When authenticated via an API token, the token may scope/pause shocker control.
        // Session auth carries no such limits.
        if (_userReferenceService.AuthReference is ApiToken apiToken)
        {
            _tokenId = apiToken.Id;
            _tokenPaused = apiToken.ShockerControlPaused;
            _tokenLimits = ApiTokenControlLimits.FromToken(apiToken);
        }
    }

    /// <summary>
    /// Apply refreshed API token limits to this connection, pushed by the <see cref="ApiTokenUpdateSubscriber"/>
    /// when the token changed or was revoked. A revoked/expired token arrives as paused with no limits.
    /// </summary>
    /// <param name="paused"></param>
    /// <param name="limits"></param>
    [NonAction]
    public void ApplyTokenLimits(bool paused, ApiTokenControlLimits? limits)
    {
        _tokenPaused = paused;
        _tokenLimits = limits;
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
    public override Guid Id => HubId ?? throw new Exception("Hub id is null");

    /// <inheritdoc />
    protected override async Task SendInitialData()
    {
        Logger.LogDebug("Starting ping timer...");
        _pingTimer.Start();

        // When authenticated via an API token, register for live updates to its limits/pause state (or revocation)
        // so changes take effect on this connection without reconnecting.
        if (_tokenId is { } tokenId)
            await _tokenUpdateSubscriber.Register(this, tokenId);

        await QueueMessage(new LiveControlResponse<LiveResponseType>
        {
            ResponseType = LiveResponseType.TPS,
            Data = new TpsData
            {
                Client = _tps
            }
        });

        await QueueMessage(new LiveControlResponse<LiveResponseType>
        {
            ResponseType = LiveResponseType.DeviceConnected
        });
    }

    /// <inheritdoc />
    protected override async Task<bool> HandleReceive(CancellationToken cancellationToken)
    {
        var message = await JsonWebSocketUtils.ReceiveFullMessageAsyncNonAlloc<BaseRequest<LiveRequestType>>(
            WebSocket!,
            LinkedToken
            );

        switch (message)
        {
            case DeserializeFailed failed:
                Logger.LogWarning(failed.Exception, "Deserialization failed for websocket message");
                await ForceClose(WebSocketCloseStatus.InvalidPayloadData, "Invalid json message received");
                return false;
            case Results.WebsocketClosure:
                Logger.LogTrace("Client sent closure");
                return false;
            default:
                var request = (BaseRequest<LiveRequestType>?)message.Value;
                if (request?.Data is null)
                {
                    Logger.LogWarning("Received null data from client");
                    await ForceClose(WebSocketCloseStatus.InvalidPayloadData, "Invalid json message received");
                    return false;
                }

                await ProcessResult(request);

                return true;
        }
    }

    private Task ProcessResult(BaseRequest<LiveRequestType> request)
        => request.RequestType switch
        {
            LiveRequestType.Pong => IntakePong(request.Data),
            LiveRequestType.Frame => IntakeFrame(request.Data),
            LiveRequestType.BulkFrame => IntakeBulkFrame(request.Data),
            _ => QueueMessage(new LiveControlResponse<LiveResponseType>()
            {
                ResponseType = LiveResponseType.RequestTypeNotFound
            }).AsTask()
        };

    /// <summary>
    /// Pong callback from the client, we can calculate latency from this
    /// </summary>
    private async Task IntakePong(JsonDocument? _)
    {
        Logger.LogTrace("Intake pong");

        // Received pong without sending ping, this could be abusing the pong endpoint.
        if (_pingTimestamp == 0)
        {
            // TODO: Kick or warn client.
            return;
        }

        _latencyMs =
            (ushort)Math.Min(Stopwatch.GetElapsedTime(_pingTimestamp).TotalMilliseconds,
                ushort.MaxValue); // If someone has a ping higher than 65 seconds, they are messing with us. Cap it to 65 seconds
        _pingTimestamp = 0;

        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("Latency: {Latency}ms", _latencyMs);

        await QueueMessage(new LiveControlResponse<LiveResponseType>
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
            frames = requestData?.Deserialize<ClientLiveFrame[]>(JsonOptions.Default);

            if (frames is not { Length: > 0 })
            {
                Logger.LogWarning("Error while deserializing bulk frame");
                await QueueMessage(new LiveControlResponse<LiveResponseType>
                {
                    ResponseType = LiveResponseType.InvalidData
                });
                return;
            }
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, "Error while deserializing frame");
            await QueueMessage(new LiveControlResponse<LiveResponseType>
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
            frame = requestData?.Deserialize<ClientLiveFrame>(JsonOptions.Default);

            if (frame is null)
            {
                Logger.LogWarning("Error while deserializing frame");
                await QueueMessage(new LiveControlResponse<LiveResponseType>
                {
                    ResponseType = LiveResponseType.InvalidData
                });
                return;
            }
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, "Error while deserializing frame");
            await QueueMessage(new LiveControlResponse<LiveResponseType>
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
        // A paused API token may not send any control, mirroring the /shockers/control endpoint.
        if (_tokenPaused)
        {
            await QueueMessage(new LiveControlResponse<LiveResponseType>
            {
                ResponseType = LiveResponseType.TokenPaused
            });

            return;
        }

        var permCheck = CheckFramePermissions(frame.Shocker, frame.Type);
        if (permCheck is not SharePermsAndLimits perms)
        {
            await QueueMessage(new LiveControlResponse<LiveResponseType>
            {
                ResponseType = permCheck switch
                {
                    Results.NotFound => LiveResponseType.ShockerNotFound,
                    LiveNotEnabled => LiveResponseType.ShockerMissingLivePermission,
                    NoPermission => LiveResponseType.ShockerMissingPermission,
                    ShockerPaused => LiveResponseType.ShockerPaused,
                    _ => throw new UnreachableException()
                }
            });

            return;
        }

        // Apply the token's intensity scoping first (if any), then clamp to the share limits.
        var scopedIntensity = _tokenLimits?.ApplyIntensity(frame.Intensity) ?? frame.Intensity;
        var intensity = Math.Clamp(scopedIntensity, HardLimits.MinControlIntensity,
            perms.Intensity ?? HardLimits.MaxControlIntensity);

        var result = HubLifetime.ReceiveFrame(frame.Shocker, frame.Type, intensity, _tps);

        switch (result)
        {
            case Results.Success:
                Logger.LogTrace("Successfully received frame");
                break;
            case Results.NotFound:
                await QueueMessage(new LiveControlResponse<LiveResponseType>
                {
                    ResponseType = LiveResponseType.ShockerNotFound
                });
                break;
            case ShockerExclusive shockerExclusive:
                await QueueMessage(new LiveControlResponse<LiveResponseType>
                {
                    ResponseType = LiveResponseType.ShockerExclusive,
                    Data = shockerExclusive.Until
                });
                break;
        }
    }

    private Results.Union5<SharePermsAndLimits, Results.NotFound, LiveNotEnabled, NoPermission, ShockerPaused> CheckFramePermissions(Guid shocker, ControlType controlType)
    {
        if (!_sharedShockers.TryGetValue(shocker, out var shockerShare)) return new Results.NotFound();

        if (shockerShare.Paused) return new ShockerPaused();
        if (!PermissionUtils.IsAllowed(controlType, true, shockerShare.PermsAndLimits)) return new NoPermission();

        return shockerShare.PermsAndLimits;
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
        await QueueMessage(new LiveControlResponse<LiveResponseType>
        {
            ResponseType = LiveResponseType.Ping,
            Data = new LcgLiveControlPing
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }
        });
    }

    /// <summary>
    /// Called by a hub lifetime when the hub is disconnected and this controller needs to die
    /// </summary>
    [NonAction]
    public async Task HubDisconnected()
    {
        Interlocked.Exchange(ref _unregistered, true);
        _unregistered = true; // The hub lifetime has already unregistered us

        Logger.LogTrace("Hub disconnected, disposing controller");

        Channel.Writer.TryComplete(); // Complete the channel so we stop sending messages

        try
        {
            await SendWebSocketMessage(new LiveControlResponse<LiveResponseType>
            {
                ResponseType = LiveResponseType.DeviceNotConnected,
            }, WebSocket!, LinkedToken);

            
            await ForceClose(WebSocketCloseStatus.NormalClosure, "Hub is disconnected");
        }
        catch (Exception e)
        {
            // We don't really care if this fails
            Logger.LogDebug(e, "Error while sending disconnect message or closing websocket");
        }
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeControllerAsync()
    {
        Logger.LogTrace("Disposing controller timer");
        _pingTimer.Dispose();

        if (_tokenId is { } tokenId)
        {
            try
            {
                await _tokenUpdateSubscriber.Unregister(this, tokenId);
            }
            catch (Exception e)
            {
                Logger.LogDebug(e, "Error unregistering from api token updates");
            }
        }

        await base.DisposeControllerAsync();
    }
}

/// <summary>
/// Union case
/// </summary>
public sealed class LiveNotEnabled;

/// <summary>
/// Union case
/// </summary>
public sealed class NoPermission;

/// <summary>
/// Union case
/// </summary>
public sealed class ShockerPaused;