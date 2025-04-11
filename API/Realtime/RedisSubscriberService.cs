using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Hubs;
using OpenShock.Common.Models.WebSocket;
using OpenShock.Common.Models.WebSocket.Device;
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
    private readonly ISubscriber _subscriber;
    private readonly IRedisCollection<DeviceOnline> _devicesOnline;

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
        _subscriber = connectionMultiplexer.GetSubscriber();
        _devicesOnline = redisConnectionProvider.RedisCollection<DeviceOnline>(false);
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
        if (data == null) return;

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
            x.Owner,
            SharedWith = x.Shockers.SelectMany(y => y.ShockerShares)
        }).FirstOrDefaultAsync();
        if (data == null) return;


        var sharedWith = await db.Users.Where(x => x.ShockerShares.Any(y => y.Shocker.Device == deviceId))
            .Select(x => x.Id).ToArrayAsync();
        var userIds = new List<string>
        {
            "local#" + data.Owner
        };
        userIds.AddRange(sharedWith.Select(x => "local#" + x));
        var deviceOnline = await _devicesOnline.FindByIdAsync(deviceId.ToString());
        var arr = new[]
        {
            new DeviceOnlineState
            {
                Device = deviceId,
                Online = deviceOnline != null,
                FirmwareVersion = deviceOnline?.FirmwareVersion ?? null
            }
        };
        await _hubContext.Clients.Users(userIds).DeviceStatus(arr);
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