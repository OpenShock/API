using StackExchange.Redis;

namespace OpenShock.Common.Services.RedisPubSub;

public static class RedisChannels
{
    public static readonly RedisChannel KeyEventExpired = new("__keyevent@0__:expired", RedisChannel.PatternMode.Literal); 
   
    public static readonly RedisChannel DeviceMessage = new("msg-device", RedisChannel.PatternMode.Literal);
}