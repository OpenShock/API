using System.Text.Json;
using OpenShock.Common.Redis.PubSub;
using OpenShock.Common.Services.RedisPubSub;
using OpenShock.Common.Utils;
using OpenShock.LiveControlGateway.LifetimeManager;
using StackExchange.Redis;

namespace OpenShock.LiveControlGateway.PubSub;

/// <summary>
/// Redis subscription service, which handles listening to pub sub on redis
/// </summary>
public sealed class RedisSubscriberService : IHostedService, IAsyncDisposable
{
    private readonly HubLifetimeManager _hubLifetimeManager;
    private readonly ISubscriber _subscriber;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="connectionMultiplexer"></param>
    /// <param name="hubLifetimeManager"></param>
    public RedisSubscriberService(
        IConnectionMultiplexer connectionMultiplexer, HubLifetimeManager hubLifetimeManager)
    {
        _hubLifetimeManager = hubLifetimeManager;
        _subscriber = connectionMultiplexer.GetSubscriber();
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _subscriber.SubscribeAsync(RedisChannels.DeviceControl,
            (_, message) => { OsTask.Run(() => DeviceControl(message)); });
        await _subscriber.SubscribeAsync(RedisChannels.DeviceCaptive,
            (_, message) => { OsTask.Run(() => DeviceControlCaptive(message)); });
        await _subscriber.SubscribeAsync(RedisChannels.DeviceUpdate,
            (_, message) => { OsTask.Run(() => DeviceUpdate(message)); });

        // OTA
        await _subscriber.SubscribeAsync(RedisChannels.DeviceOtaInstall,
            (_, message) => { OsTask.Run(() => DeviceOtaInstall(message)); });
    }

    private async Task DeviceControl(RedisValue value)
    {
        if (!value.HasValue) return;
        var data = JsonSerializer.Deserialize<ControlMessage>(value.ToString());
        if (data == null) return;

        await Task.WhenAll(data.ControlMessages.Select(x => _hubLifetimeManager.Control(x.Key, x.Value)));
    }

    private async Task DeviceControlCaptive(RedisValue value)
    {
        if (!value.HasValue) return;
        var data = JsonSerializer.Deserialize<CaptiveMessage>(value.ToString());
        if (data == null) return;

        await _hubLifetimeManager.ControlCaptive(data.DeviceId, data.Enabled);
    }

    /// <summary>
    /// Update the device connection if found
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private async Task DeviceUpdate(RedisValue value)
    {
        if (!value.HasValue) return;
        var data = JsonSerializer.Deserialize<DeviceUpdatedMessage>(value.ToString());
        if (data == null) return;
        
        await _hubLifetimeManager.UpdateDevice(data.Id);
    }

    /// <summary>
    /// Update the device connection if found
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private async Task DeviceOtaInstall(RedisValue value)
    {
        if (!value.HasValue) return;
        var data = JsonSerializer.Deserialize<DeviceOtaInstallMessage>(value.ToString());
        if (data == null) return;
        
        await _hubLifetimeManager.OtaInstall(data.Id, data.Version);
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

    /// <summary>
    /// Destructor, just in case
    /// </summary>
    ~RedisSubscriberService()
    {
        DisposeAsync().AsTask().Wait();
    }
}