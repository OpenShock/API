using System.Text.Json;
using MessagePack;
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
        await _subscriber.SubscribeAsync(RedisChannels.DeviceMessage, (_, val) => OsTask.Run(() => DeviceMessage(val)));
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task DeviceMessage(RedisValue value)
    {
        if (!value.HasValue) return;
        var message = MessagePackSerializer.Deserialize<DeviceMessage>(Convert.FromBase64String(value.ToString()));
        switch (message.Type)
        {
            case DeviceMessageType.Trigger:
                await DeviceMessageTrigger(message.DeviceId, message.Payload as DeviceTriggerPayload);
                break;
            case DeviceMessageType.Toggle:
                await DeviceMessageToggle(message.DeviceId, message.Payload as DeviceTogglePayload);
                break;
            case DeviceMessageType.Control:
                await DeviceMessageControl(message.DeviceId, message.Payload as DeviceControlPayload);
                break;
            case DeviceMessageType.OtaInstall:
                await DeviceMessageOtaInstall(message.DeviceId, message.Payload as DeviceOtaInstallPayload);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task DeviceMessageTrigger(Guid deviceId, DeviceTriggerPayload? payload)
    {
        if (payload is null) return;
        switch (payload.Type)
        {
            case DeviceTriggerType.DeviceInfoUpdated:
                await _hubLifetimeManager.UpdateDevice(deviceId);
                break;
            case DeviceTriggerType.DeviceEmergencyStop:
                await _hubLifetimeManager.EmergencyStop(deviceId);
                break;
            case DeviceTriggerType.DeviceReboot:
                await _hubLifetimeManager.Reboot(deviceId);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task DeviceMessageToggle(Guid deviceId, DeviceTogglePayload? payload)
    {
        if (payload is null) return;
        switch (payload.Target)
        {
            case DeviceToggleTarget.CaptivePortal:
                await _hubLifetimeManager.ControlCaptive(deviceId, payload.State);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task DeviceMessageControl(Guid deviceId, DeviceControlPayload? payload)
    {
        if (payload is null) return;
        await Task.WhenAll(payload.Controls.Select(x => _hubLifetimeManager.Control(deviceId, x)));
    }

    private async Task DeviceMessageOtaInstall(Guid deviceId, DeviceOtaInstallPayload? payload)
    {
        if (payload is null) return;
        await _hubLifetimeManager.OtaInstall(deviceId, payload.Version);
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