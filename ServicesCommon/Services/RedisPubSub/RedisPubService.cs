using System.Text.Json;
using OpenShock.Common.Redis.PubSub;
using Semver;
using StackExchange.Redis;

namespace OpenShock.ServicesCommon.Services.RedisPubSub;

public sealed class RedisPubService : IRedisPubService
{
    private readonly ISubscriber _subscriber;
    
    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="connectionMultiplexer"></param>
    public RedisPubService(IConnectionMultiplexer connectionMultiplexer)
    {
        _subscriber = connectionMultiplexer.GetSubscriber();
    }
    
    /// <inheritdoc />
    public Task SendDeviceControl(Guid sender, IDictionary<Guid, IList<ControlMessage.ShockerControlInfo>> controlMessages)
    {
        var redisMessage = new ControlMessage
        {
            Sender = sender,
            ControlMessages = controlMessages
        };

        return _subscriber.PublishAsync(RedisChannels.DeviceControl, JsonSerializer.Serialize(redisMessage));
    }

    /// <inheritdoc />
    public Task SendDeviceCaptivePortal(Guid deviceId, bool enabled)
    {
        var redisMessage = new CaptiveMessage
        {
            DeviceId = deviceId,
            Enabled = enabled
        };

        return _subscriber.PublishAsync(RedisChannels.DeviceCaptive, JsonSerializer.Serialize(redisMessage));
    }

    /// <inheritdoc />
    public Task SendDeviceUpdate(Guid deviceId)
    {
        var redisMessage = new DeviceUpdatedMessage
        {
            Id = deviceId
        };

        return _subscriber.PublishAsync(RedisChannels.DeviceUpdate, JsonSerializer.Serialize(redisMessage));
    }

    /// <inheritdoc />
    public Task SendDeviceOtaInstall(Guid deviceId, SemVersion version)
    {
        var redisMessage = new DeviceOtaInstallMessage
        {
            Id = deviceId,
            Version = version
        };
        
        return _subscriber.PublishAsync(RedisChannels.DeviceOtaInstall, JsonSerializer.Serialize(redisMessage));
    }
}