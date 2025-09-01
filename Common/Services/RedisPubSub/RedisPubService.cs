using MessagePack;
using OpenShock.Common.Models;
using OpenShock.Common.Redis.PubSub;
using StackExchange.Redis;

namespace OpenShock.Common.Services.RedisPubSub;

public sealed class RedisPubService : IRedisPubService
{
    private readonly ISubscriber _subscriber;

    public RedisPubService(IConnectionMultiplexer connectionMultiplexer)
    {
        _subscriber = connectionMultiplexer.GetSubscriber();
    }

    private Task<long> Publish<T>(RedisChannel channel, T msg) => _subscriber.PublishAsync(channel, (RedisValue)new ReadOnlyMemory<byte>(MessagePackSerializer.Serialize(msg)));
    private Task<long> PublishMessage(Guid deviceId, DeviceMessage msg) => Publish(RedisChannels.DeviceMessage(deviceId), msg);

    public Task SendDeviceUpdate(Guid deviceId)
    {
        return PublishMessage(deviceId, DeviceMessage.Create(DeviceTriggerType.DeviceInfoUpdated));
    }

    public Task SendDeviceOnlineStatus(Guid deviceId, bool isOnline)
    {
        return Publish(RedisChannels.DeviceStatus, DeviceStatus.Create(deviceId, DeviceBoolStateType.Online, isOnline));
    }

    public Task SendDeviceEstoppedStatus(Guid deviceId, bool isEstopped)
    {
        return Publish(RedisChannels.DeviceStatus, DeviceStatus.Create(deviceId, DeviceBoolStateType.EStopped, isEstopped));
    }

    public Task SendDeviceControl(Guid deviceId, List<ShockerControlCommand> controls)
    {
        return PublishMessage(deviceId, DeviceMessage.Create(new DeviceControlPayload { Controls = controls }));
    }

    public Task SendDeviceCaptivePortal(Guid deviceId, bool enabled)
    {
        return PublishMessage(deviceId, DeviceMessage.Create(DeviceToggleTarget.CaptivePortal, enabled));
    }

    public Task SendDeviceEmergencyStop(Guid deviceId)
    {
        return PublishMessage(deviceId, DeviceMessage.Create(DeviceTriggerType.DeviceEmergencyStop));
    }

    public Task SendDeviceOtaInstall(Guid deviceId, SemVersion version)
    {
        return PublishMessage(deviceId, DeviceMessage.Create(new DeviceOtaInstallPayload { Version = version }));
    }

    public Task SendDeviceReboot(Guid deviceId)
    {
        return PublishMessage(deviceId, DeviceMessage.Create(DeviceTriggerType.DeviceReboot));
    }
}