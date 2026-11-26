using MessagePack;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Hubs;
using OpenShock.Common.Models.WebSocket;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.Common.Redis.PubSub;
using OpenShock.Common.Services.RedisPubSub;
using OpenShock.Internal.Common.Utils;
using Redis.OM.Contracts;
using StackExchange.Redis;

namespace OpenShock.API.Realtime;

/// <summary>
/// Redis subscription service, which handles listening to pub sub on redis
/// </summary>
public sealed class RedisSubscriberService : IHostedService, IAsyncDisposable
{
    private readonly IHubContext<UserHub, IUserHub> _hubContext;
    private readonly IDbContextFactory<OpenShockContext> _dbContextFactory;
    private readonly IRedisConnectionProvider _redisConnectionProvider;
    private readonly HubConnectionTracker _hubConnections;
    private readonly ISubscriber _subscriber;
    private readonly ILogger<RedisSubscriberService> _logger;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="connectionMultiplexer"></param>
    /// <param name="hubContext"></param>
    /// <param name="dbContextFactory"></param>
    /// <param name="redisConnectionProvider"></param>
    /// <param name="hubConnections">The hub connections open on this instance, closed here when what they authenticated with is revoked.</param>
    /// <param name="logger"></param>
    public RedisSubscriberService(
        IConnectionMultiplexer connectionMultiplexer,
        IHubContext<UserHub, IUserHub> hubContext,
        IDbContextFactory<OpenShockContext> dbContextFactory,
        IRedisConnectionProvider redisConnectionProvider,
        HubConnectionTracker hubConnections,
        ILogger<RedisSubscriberService> logger
        )
    {
        _hubContext = hubContext;
        _dbContextFactory = dbContextFactory;
        _redisConnectionProvider = redisConnectionProvider;
        _hubConnections = hubConnections;
        _subscriber = connectionMultiplexer.GetSubscriber();
        _logger = logger;
    }
    
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _subscriber.SubscribeAsync(RedisChannels.KeyEventExpired, HandleKeyExpired);
        await _subscriber.SubscribeAsync(RedisChannels.DeviceStatus, HandleDeviceStatus);
        await _subscriber.SubscribeAsync(RedisChannels.SessionRevoked, HandleSessionRevoked);
        await _subscriber.SubscribeAsync(RedisChannels.UserAuthRevoked, HandleUserAuthRevoked);
        await _subscriber.SubscribeAsync(RedisChannels.ApiTokenUpdateAll, HandleApiTokenUpdate);
    }

    private void HandleSessionRevoked(RedisChannel _, RedisValue message)
    {
        var sessionId = (string?)message;
        if (string.IsNullOrEmpty(sessionId)) return;

        var closed = _hubConnections.AbortSession(sessionId);
        if (closed > 0) _logger.LogDebug("Closed {Count} hub connection(s) of a deleted session", closed);
    }

    private void HandleUserAuthRevoked(RedisChannel _, RedisValue message)
    {
        if (!Guid.TryParse((string?)message, out var userId)) return;

        var closed = _hubConnections.AbortUser(userId);
        if (closed > 0) _logger.LogDebug("Closed {Count} hub connection(s) of user [{UserId}]", closed, userId);
    }

    private void HandleApiTokenUpdate(RedisChannel channel, RedisValue _)
    {
        var channelName = (string?)channel;
        if (channelName is null || !channelName.StartsWith(RedisChannels.ApiTokenUpdatePrefix, StringComparison.Ordinal)) return;
        if (!Guid.TryParse(channelName.AsSpan(RedisChannels.ApiTokenUpdatePrefix.Length), out var tokenId)) return;

        // Most token changes concern no connection on this instance: no database round trip for those.
        if (!_hubConnections.HasApiTokenConnections(tokenId)) return;

        OsTask.Run(() => ApplyApiTokenUpdate(tokenId));
    }

    /// <summary>
    /// The same channel announces an edit and a deletion, so look: a token that is gone closes its
    /// connections, one that changed makes them read it again on their next call.
    /// </summary>
    private async Task ApplyApiTokenUpdate(Guid tokenId)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var stillValid = await db.ApiTokens.AnyAsync(x =>
            x.Id == tokenId &&
            (x.ValidUntil == null || x.ValidUntil >= DateTime.UtcNow) &&
            x.User.UserDeactivation == null);

        if (stillValid)
        {
            _hubConnections.InvalidateApiToken(tokenId);
            return;
        }

        var closed = _hubConnections.AbortApiToken(tokenId);
        _logger.LogDebug("Closed {Count} hub connection(s) of revoked API token [{TokenId}]", closed, tokenId);
    }

    private void HandleKeyExpired(RedisChannel _, RedisValue message)
    {
        if (!message.HasValue)
        {
            _logger.LogWarning("Received expired key with empty value for hub offline status");
            return;
        }
        
        var messageString = (string?)message;
        if (messageString is null)
        {
            _logger.LogWarning("Received expired key that could not be converted to string for hub offline status. Raw value type: {ValueType}", message.GetType().FullName);
            return;
        }
        
        var messageSpan = messageString.AsSpan();
        
        // We always expect TypeName:GUID right now, if GUID is not present, something is really wrong
        var colonPos = messageSpan.IndexOf(':');
        if (colonPos < 0)
        {
            _logger.LogError("Received expired key with unexpected format (missing colon) for hub offline status. Value: {MessageValue}", messageString);
            return;
        }
        
        // Data structure is TypeName:GUID
        var typeNameSpan = messageSpan[..colonPos];
        var guidSpan = messageSpan[(colonPos + 1)..];
        
        // Check what type of expired key this is
        if (typeNameSpan.SequenceEqual(typeof(DeviceOnline).FullName))
        {
            if (!Guid.TryParse(guidSpan, out var id))
            {
                _logger.LogError("Received expired key with invalid GUID for hub offline status: {MessageValue}", messageString);
                return;
            }
            
            OsTask.Run(() => LogicDeviceOnlineStatus(id));
        }
        else if (typeNameSpan.SequenceEqual(typeof(LoginSession).FullName))
        {
            // A session that reached its lifetime: same as a deleted one for the connections using it.
            _hubConnections.AbortSession(guidSpan.ToString());
        }
    }

    private void HandleDeviceStatus(RedisChannel _, RedisValue value)
    {
        if (!value.HasValue) return;

        DeviceStatus message;
        try
        {
            message = MessagePackSerializer.Deserialize<DeviceStatus>(value);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to deserialize redis message");
            return;
        }

        OsTask.Run(() => HandleDeviceStatusMessage(message));
    }

    private async Task HandleDeviceStatusMessage(DeviceStatus message)
    {
        // DeviceStatusPayload is closed, so a payload type added without a case here is a compile warning
        await (message.Payload switch
        {
            DeviceBoolStatePayload boolState => HandleDeviceBoolState(message.DeviceId, boolState),
        });
    }

    private async Task HandleDeviceBoolState(Guid deviceId, DeviceBoolStatePayload state)
    {
        switch (state.Type)
        {
            case DeviceBoolStateType.Online:
                await LogicDeviceOnlineStatus(deviceId); // TODO: Handle device offline messages too
                break;
            case DeviceBoolStateType.EStopped:
                _logger.LogInformation("EStopped state not implemented yet for DeviceId {DeviceId}", deviceId);
                break;
            default:
                _logger.LogError("Unknown DeviceBoolStateType: {StateType}", state.Type);
                break;
        }
    }

    private async Task LogicDeviceOnlineStatus(Guid deviceId)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        
        var data = await db.Devices.Where(x => x.Id == deviceId).Select(x => new
        {
            x.OwnerId,
            SharedWith = x.Shockers.SelectMany(y => y.UserShares)
        }).FirstOrDefaultAsync();
        if (data is null) return;


        var sharedWith = await db.Shockers
            .Where(s => s.DeviceId == deviceId)
            .SelectMany(s => s.UserShares)
            .Select(u => u.SharedWithUserId)
            .ToArrayAsync();
        var userIds = new List<string>
        {
            "local#" + data.OwnerId
        };
        userIds.AddRange(sharedWith.Select(x => "local#" + x));
        
        var devicesOnlineCollection = _redisConnectionProvider.RedisCollection<DeviceOnline>(false);
        var deviceOnline = await devicesOnlineCollection.FindByIdAsync(deviceId.ToString());
        
        await _hubContext.Clients.Users(userIds).DeviceStatus([
            new DeviceOnlineState
            {
                Device = deviceId,
                Online = deviceOnline is not null,
                FirmwareVersion = deviceOnline?.FirmwareVersion ?? null
            }
        ]);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _subscriber.UnsubscribeAllAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            await _subscriber.UnsubscribeAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Redis unsubscribe in DisposeAsync");
        }

        GC.SuppressFinalize(this);
    }

    ~RedisSubscriberService()
    {
        DisposeAsync().AsTask().Wait();
    }
}
