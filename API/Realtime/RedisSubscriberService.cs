using MessagePack;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Hubs;
using OpenShock.Common.Models.WebSocket;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.Common.Redis.PubSub;
using OpenShock.Common.Services.RedisPubSub;
using OpenShock.Common.Utils;
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
    private readonly ISubscriber _subscriber;
    private readonly ILogger<RedisSubscriberService> _logger;

    private ChannelMessageQueue? _expiredQueue;
    private ChannelMessageQueue? _deviceStatusQueue;
    private CancellationTokenSource? _cts;
    private Task? _expiredConsumerTask;
    private Task? _deviceConsumerTask;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="connectionMultiplexer"></param>
    /// <param name="hubContext"></param>
    /// <param name="dbContextFactory"></param>
    /// <param name="redisConnectionProvider"></param>
    /// <param name="logger"></param>
    public RedisSubscriberService(
        IConnectionMultiplexer connectionMultiplexer,
        IHubContext<UserHub, IUserHub> hubContext,
        IDbContextFactory<OpenShockContext> dbContextFactory,
        IRedisConnectionProvider redisConnectionProvider,
        ILogger<RedisSubscriberService> logger
        )
    {
        _hubContext = hubContext;
        _dbContextFactory = dbContextFactory;
        _redisConnectionProvider = redisConnectionProvider;
        _subscriber = connectionMultiplexer.GetSubscriber();
        _logger = logger;
    }
    
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _expiredQueue = await _subscriber.SubscribeAsync(RedisChannels.KeyEventExpired);
        _deviceStatusQueue = await _subscriber.SubscribeAsync(RedisChannels.DeviceStatus);

        _expiredConsumerTask = QueueHelper.ConsumeQueue(_expiredQueue, HandleKeyExpired, _logger, _cts.Token);
        _deviceConsumerTask = QueueHelper.ConsumeQueue(_deviceStatusQueue, HandleDeviceStatus, _logger, _cts.Token);
    }

    private async Task HandleKeyExpired(RedisValue value, CancellationToken cancellationToken)
    {
        if (value.ToString().Split(':', 2) is not [string guid, string name]) return;

        if (!Guid.TryParse(guid, out var id)) return;

        if (typeof(DeviceOnline).FullName == name)
        {
            await LogicDeviceOnlineStatus(id, cancellationToken);
        }
    }

    private async Task HandleDeviceStatus(RedisValue value, CancellationToken cancellationToken)
    {
        if (!value.HasValue) return;

        DeviceStatus message;
        try
        {
            message = MessagePackSerializer.Deserialize<DeviceStatus>((ReadOnlyMemory<byte>)value, cancellationToken: cancellationToken);
            if (message is null) return;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to deserialize redis message");
            return;
        }

        switch (message.Payload)
        {
            case DeviceBoolStatePayload boolState:
                await HandleDeviceBoolState(message.DeviceId, boolState, cancellationToken);
                break;
            default:
                _logger.LogError("Got DeviceStatus with unknown payload type: {PayloadType}", message.Payload?.GetType().Name);
                break;
        }

    }

    private async Task HandleDeviceBoolState(Guid deviceId, DeviceBoolStatePayload state, CancellationToken cancellationToken)
    {
        switch (state.Type)
        {
            case DeviceBoolStateType.Online:
                await LogicDeviceOnlineStatus(deviceId, cancellationToken); // TODO: Handle device offline messages too
                break;
            case DeviceBoolStateType.EStopped:
                _logger.LogInformation("EStopped state not implemented yet for DeviceId {DeviceId}", deviceId);
                break;
            default:
                _logger.LogError("Unknown DeviceBoolStateType: {StateType}", state.Type);
                break;
        }
    }

    private async Task LogicDeviceOnlineStatus(Guid deviceId, CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var data = await db.Devices
            .Where(x => x.Id == deviceId)
            .Select(x => new
            {
                x.OwnerId,
                SharedWith = x.Shockers.SelectMany(y => y.UserShares)
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (data is null) return;


        var sharedWith = await db.Shockers
            .Where(s => s.DeviceId == deviceId)
            .SelectMany(s => s.UserShares)
            .Select(u => u.SharedWithUserId)
            .ToArrayAsync(cancellationToken);
        var userIds = new List<string>
        {
            "local#" + data.OwnerId
        };
        userIds.AddRange(sharedWith.Select(x => "local#" + x));
        
        var devicesOnlineCollection = _redisConnectionProvider.RedisCollection<DeviceOnline>(false);
        var deviceOnline = await devicesOnlineCollection.FindByIdAsync(deviceId.ToString());
        
        await _hubContext.Clients
            .Users(userIds)
            .DeviceStatus([
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
        // Cancel consumers first, then unsubscribe
        try
        {
            _cts?.Cancel();
        }
        catch { /* ignore */ }

        // Wait for loops to finish
        if (_expiredConsumerTask is not null)
        {
            try { await _expiredConsumerTask; } catch { /* ignore */ }
        }
        if (_deviceConsumerTask is not null)
        {
            try { await _deviceConsumerTask; } catch { /* ignore */ }
        }

        if (_expiredQueue is not null)
        {
            await _subscriber.UnsubscribeAsync(_expiredQueue.Channel);
        }
        if (_deviceStatusQueue is not null)
        {
            await _subscriber.UnsubscribeAsync(_deviceStatusQueue.Channel);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            await StopAsync(default);
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