using StackExchange.Redis;

namespace OpenShock.ServicesCommon.Services.RedisPubSub;

public static class RedisChannels
{
    public static readonly RedisChannel KeyEventExpired = new("__keyevent@0__:expired", RedisChannel.PatternMode.Literal);
    public static readonly RedisChannel KeyEventJsonSet = new("__keyevent@0__:json.set", RedisChannel.PatternMode.Literal);
    public static readonly RedisChannel KeyEventDel = new("__keyevent@0__:del", RedisChannel.PatternMode.Literal);

    public static readonly RedisChannel DeviceControl = new("msg-device-control", RedisChannel.PatternMode.Literal);
    public static readonly RedisChannel DeviceCaptive = new("msg-device-control-captive", RedisChannel.PatternMode.Literal);
    public static readonly RedisChannel DeviceUpdate = new("msg-device-update", RedisChannel.PatternMode.Literal);

    // OTA
    public static readonly RedisChannel DeviceOtaInstall = new("msg-device-ota-install", RedisChannel.PatternMode.Literal);
}