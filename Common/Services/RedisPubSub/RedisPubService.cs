using MessagePack;
using OpenShock.Common.Redis.PubSub;
using Semver;
using StackExchange.Redis;

namespace OpenShock.Common.Services.RedisPubSub;

public sealed class RedisPubService : IRedisPubService
{
    private readonly ISubscriber _subscriber;

    public RedisPubService(IConnectionMultiplexer connectionMultiplexer)
    {
        _subscriber = connectionMultiplexer.GetSubscriber();
    }

    public Task SendDeviceUpdate(Guid deviceId)
    {
        var msg = DeviceMessage.Create(deviceId, DeviceTriggerType.DeviceInfoUpdated);
        var bytes = MessagePackSerializer.Serialize(msg);
        return _subscriber.PublishAsync(RedisChannels.DeviceMessage, bytes);
    }

    public Task SendDeviceOnlineStatus(Guid deviceId, bool isOnline)
    {
        var msg = DeviceMessage.Create(deviceId, ToggleTarget.DeviceOnline, isOnline);
        var bytes = MessagePackSerializer.Serialize(msg);
        return _subscriber.PublishAsync(RedisChannels.DeviceMessage, bytes);
    }

    public Task SendDeviceControl(Guid deviceId, Guid senderId, DeviceControlPayload.ShockerControlInfo[] controls)
    {
        var msg = DeviceMessage.Create(deviceId, new DeviceControlPayload { Sender = senderId, Controls = controls });
        var bytes = MessagePackSerializer.Serialize(msg);
        return _subscriber.PublishAsync(RedisChannels.DeviceMessage, bytes);
    }

    public Task SendDeviceCaptivePortal(Guid deviceId, bool enabled)
    {
        var msg = DeviceMessage.Create(deviceId, ToggleTarget.CaptivePortal, enabled);
        var bytes = MessagePackSerializer.Serialize(msg);
        return _subscriber.PublishAsync(RedisChannels.DeviceMessage, bytes);
    }

    public Task SendDeviceEmergencyStop(Guid deviceId)
    {
        var msg = DeviceMessage.Create(deviceId, DeviceTriggerType.DeviceEmergencyStop);
        var bytes = MessagePackSerializer.Serialize(msg);
        return _subscriber.PublishAsync(RedisChannels.DeviceMessage, bytes);
    }

    public Task SendDeviceOtaInstall(Guid deviceId, SemVersion version)
    {
        var msg = DeviceMessage.Create(deviceId, new DeviceOtaInstallPayload { Version = version });
        var bytes = MessagePackSerializer.Serialize(msg);
        return _subscriber.PublishAsync(RedisChannels.DeviceMessage, bytes);
    }

    public Task SendDeviceReboot(Guid deviceId)
    {
        var msg = DeviceMessage.Create(deviceId, DeviceTriggerType.DeviceReboot);
        var bytes = MessagePackSerializer.Serialize(msg);
        return _subscriber.PublishAsync(RedisChannels.DeviceMessage, bytes);
    }
}