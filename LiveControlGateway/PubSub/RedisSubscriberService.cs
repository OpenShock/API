using OpenShock.Common.Redis.PubSub;
using OpenShock.Common.Utils;
using OpenShock.LiveControlGateway.LifetimeManager;
using OpenShock.ServicesCommon.Services.RedisPubSub;
using StackExchange.Redis;
using System.Text.Json;

namespace OpenShock.LiveControlGateway.PubSub;

/// <summary>
/// Redis subscription service, which handles listening to pub sub on redis
/// </summary>
public sealed class RedisSubscriberService : IHostedService, IAsyncDisposable
{
    private readonly ISubscriber _subscriber;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="connectionMultiplexer"></param>
    public RedisSubscriberService(
        IConnectionMultiplexer connectionMultiplexer)
    {
        _subscriber = connectionMultiplexer.GetSubscriber();
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _subscriber.SubscribeAsync(RedisChannels.DeviceControl,
            (_, message) => { LucTask.Run(() => DeviceControl(message)); });
        await _subscriber.SubscribeAsync(RedisChannels.DeviceCaptive,
            (_, message) => { LucTask.Run(() => DeviceControlCaptive(message)); });
        await _subscriber.SubscribeAsync(RedisChannels.DeviceUpdate,
            (_, message) => { LucTask.Run(() => DeviceUpdate(message)); });

        // OTA
        await _subscriber.SubscribeAsync(RedisChannels.DeviceOtaInstall,
            (_, message) => { LucTask.Run(() => DeviceOtaInstall(message)); });
    }

    private static async Task DeviceControl(RedisValue value)
    {
        if (!value.HasValue) return;
        var data = JsonSerializer.Deserialize<ControlMessage>(value.ToString());
        if (data == null) return;

        await Task.WhenAll(data.ControlMessages.Select(x => DeviceLifetimeManager.Control(x.Key, x.Value)));
    }

    private static async Task DeviceControlCaptive(RedisValue value)
    {
        if (!value.HasValue) return;
        var data = JsonSerializer.Deserialize<CaptiveMessage>(value.ToString());
        if (data == null) return;

        await DeviceLifetimeManager.ControlCaptive(data.DeviceId, data.Enabled);
    }

    /// <summary>
    /// Update the device connection if found
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private static async Task DeviceUpdate(RedisValue value)
    {
        if (!value.HasValue) return;
        var data = JsonSerializer.Deserialize<DeviceUpdatedMessage>(value.ToString());
        if (data == null) return;

        await DeviceLifetimeManager.UpdateDevice(data.Id);
    }

    /// <summary>
    /// Update the device connection if found
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private static async Task DeviceOtaInstall(RedisValue value)
    {
        if (!value.HasValue) return;
        var data = JsonSerializer.Deserialize<DeviceOtaInstallMessage>(value.ToString());
        if (data == null) return;

        await DeviceLifetimeManager.OtaInstall(data.Id, data.Version);
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