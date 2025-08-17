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

    private Task<long> Publish<T>(Guid deviceId, T msg) => _subscriber.PublishAsync(RedisChannels.DeviceMessage(deviceId),
        Convert.ToBase64String(MessagePackSerializer.Serialize(msg)));

    public Task SendDeviceUpdate(Guid deviceId)
    {
        return Publish(deviceId, DeviceMessage.Create(DeviceTriggerType.DeviceInfoUpdated));
    }

    public Task SendDeviceOnlineStatus(Guid deviceId, bool isOnline)
    {
        return Publish(deviceId, DeviceStatus.Create(DeviceBoolStateType.Online, isOnline));
    }

    public Task SendDeviceControl(Guid deviceId, DeviceControlPayload.ShockerControlInfo[] controls)
    {
        return Publish(deviceId, DeviceMessage.Create(new DeviceControlPayload { Controls = controls }));
    }

    public Task SendDeviceCaptivePortal(Guid deviceId, bool enabled)
    {
        return Publish(deviceId, DeviceMessage.Create(DeviceToggleTarget.CaptivePortal, enabled));
    }

    public Task SendDeviceEmergencyStop(Guid deviceId)
    {
        return Publish(deviceId, DeviceMessage.Create(DeviceTriggerType.DeviceEmergencyStop));
    }

    public Task SendDeviceOtaInstall(Guid deviceId, SemVersion version)
    {
        return Publish(deviceId, DeviceMessage.Create(new DeviceOtaInstallPayload { Version = version }));
    }

    public Task SendDeviceReboot(Guid deviceId)
    {
        return Publish(deviceId, DeviceMessage.Create(DeviceTriggerType.DeviceReboot));
    }
}