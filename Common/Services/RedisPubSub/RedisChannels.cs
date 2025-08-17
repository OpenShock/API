using StackExchange.Redis;

namespace OpenShock.Common.Services.RedisPubSub;

public static class RedisChannels
{
    public static readonly RedisChannel KeyEventExpired = new("__keyevent@0__:expired", RedisChannel.PatternMode.Literal); 
   
    public static RedisChannel DeviceMessage(Guid deviceId) => new($"device-msg:{deviceId}", RedisChannel.PatternMode.Pattern);
   
    public static readonly RedisChannel DeviceStatus = new("device-status", RedisChannel.PatternMode.Literal);
}