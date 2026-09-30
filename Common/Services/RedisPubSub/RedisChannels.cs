using StackExchange.Redis;

namespace OpenShock.Common.Services.RedisPubSub;

public static class RedisChannels
{
    public static readonly RedisChannel KeyEventExpired = new("__keyevent@0__:expired", RedisChannel.PatternMode.Literal); 
   
    public static RedisChannel DeviceMessage(Guid deviceId) => new($"device-msg:{deviceId}", RedisChannel.PatternMode.Literal);

    public static readonly RedisChannel DeviceStatus = new("device-status", RedisChannel.PatternMode.Literal);

    public static RedisChannel ApiTokenUpdate(Guid tokenId) => new($"api-token-update:{tokenId}", RedisChannel.PatternMode.Literal);

    /// <summary>
    /// Fired when a row is added to the email outbox, so the email consumer drains it immediately
    /// instead of waiting for its next poll. Payload-less: it is purely a "wake up and check" nudge.
    /// </summary>
    public static readonly RedisChannel EmailOutboxPending = new("email-outbox-pending", RedisChannel.PatternMode.Literal);
}