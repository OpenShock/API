using StackExchange.Redis;

namespace OpenShock.Common.Services.RedisPubSub;

public static class RedisChannels
{
    public static readonly RedisChannel KeyEventExpired = new("__keyevent@0__:expired", RedisChannel.PatternMode.Literal); 
   
    public static readonly RedisChannel DeviceControl = new("msg-device-control", RedisChannel.PatternMode.Literal);
    public static readonly RedisChannel DeviceCaptive = new("msg-device-control-captive", RedisChannel.PatternMode.Literal);
    public static readonly RedisChannel DeviceUpdate = new("msg-device-update", RedisChannel.PatternMode.Literal);
    public static readonly RedisChannel DeviceOnlineStatus = new("msg-device-online-status", RedisChannel.PatternMode.Literal);
    
    // OTA
    public static readonly RedisChannel DeviceOtaInstall = new("msg-device-ota-install", RedisChannel.PatternMode.Literal);
    
    public static readonly RedisChannel DeviceEmergencyStop = new("msg-device-emergency-stop", RedisChannel.PatternMode.Literal);
    public static readonly RedisChannel DeviceReboot = new("msg-device-reboot", RedisChannel.PatternMode.Literal);
}