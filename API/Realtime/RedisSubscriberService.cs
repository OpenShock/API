using System.Text.Json;
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
using Redis.OM.Searching;
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

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="connectionMultiplexer"></param>
    /// <param name="hubContext"></param>
    /// <param name="dbContextFactory"></param>
    /// <param name="redisConnectionProvider"></param>
    public RedisSubscriberService(
        IConnectionMultiplexer connectionMultiplexer,
        IHubContext<UserHub, IUserHub> hubContext,
        IDbContextFactory<OpenShockContext> dbContextFactory,
        IRedisConnectionProvider redisConnectionProvider)
    {
        _hubContext = hubContext;
        _dbContextFactory = dbContextFactory;
        _redisConnectionProvider = redisConnectionProvider;
        _subscriber = connectionMultiplexer.GetSubscriber();
    }
    
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _subscriber.SubscribeAsync(RedisChannels.KeyEventExpired, (_, message) => { OsTask.Run(() => HandleKeyExpired(message)); });
        await _subscriber.SubscribeAsync(RedisChannels.DeviceOnlineStatus, (_, message) => { OsTask.Run(() => HandleDeviceOnlineStatus(message)); });
    }

    private async Task HandleDeviceOnlineStatus(RedisValue message)
    {
        if (!message.HasValue) return;
        var data = JsonSerializer.Deserialize<DeviceUpdatedMessage>(message.ToString());
        if (data is null) return;

        await LogicDeviceOnlineStatus(data.Id);
    }
    
    private async Task HandleKeyExpired(RedisValue message)
    {
        if (!message.HasValue) return;
        var msg = message.ToString().Split(':');
        if (msg.Length < 2) return;


        if (!Guid.TryParse(msg[1], out var id)) return;

        if (typeof(DeviceOnline).FullName == msg[0])
        {
            await LogicDeviceOnlineStatus(id);
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
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _subscriber.UnsubscribeAllAsync();
        GC.SuppressFinalize(this);
    }

    ~RedisSubscriberService()
    {
        DisposeAsync().AsTask().Wait();
    }
}