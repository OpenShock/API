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

    private Task<long> Publish<T>(T msg) => _subscriber.PublishAsync(RedisChannels.DeviceMessage,
        Convert.ToBase64String(MessagePackSerializer.Serialize(msg)));

    public Task SendDeviceUpdate(Guid deviceId)
    {
        return Publish(DeviceMessage.Create(deviceId, DeviceTriggerType.DeviceInfoUpdated));
    }

    public Task SendDeviceOnlineStatus(Guid deviceId, bool isOnline)
    {
        return Publish(DeviceStatus.Create(deviceId, DeviceStatusType.Online));
    }

    public Task SendDeviceControl(Guid deviceId, Guid senderId, DeviceControlPayload.ShockerControlInfo[] controls)
    {
        return Publish(DeviceMessage.Create(deviceId, new DeviceControlPayload { Sender = senderId, Controls = controls }));
    }

    public Task SendDeviceCaptivePortal(Guid deviceId, bool enabled)
    {
        return Publish(DeviceMessage.Create(deviceId, DeviceToggleTarget.CaptivePortal, enabled));
    }

    public Task SendDeviceEmergencyStop(Guid deviceId)
    {
        return Publish(DeviceMessage.Create(deviceId, DeviceTriggerType.DeviceEmergencyStop));
    }

    public Task SendDeviceOtaInstall(Guid deviceId, SemVersion version)
    {
        return Publish(DeviceMessage.Create(deviceId, new DeviceOtaInstallPayload { Version = version }));
    }

    public Task SendDeviceReboot(Guid deviceId)
    {
        return Publish(DeviceMessage.Create(deviceId, DeviceTriggerType.DeviceReboot));
    }
}