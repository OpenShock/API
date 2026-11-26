using StackExchange.Redis;

namespace OpenShock.Common.Services.RedisPubSub;

public static class RedisChannels
{
    public static readonly RedisChannel KeyEventExpired = new("__keyevent@0__:expired", RedisChannel.PatternMode.Literal); 
   
    public static RedisChannel DeviceMessage(Guid deviceId) => new($"device-msg:{deviceId}", RedisChannel.PatternMode.Literal);

    public static readonly RedisChannel DeviceStatus = new("device-status", RedisChannel.PatternMode.Literal);

    public static RedisChannel ApiTokenUpdate(Guid tokenId) => new($"api-token-update:{tokenId}", RedisChannel.PatternMode.Literal);

    /// <summary>Every <see cref="ApiTokenUpdate"/> channel, for a subscriber that cares about all tokens.</summary>
    public static readonly RedisChannel ApiTokenUpdateAll = new("api-token-update:*", RedisChannel.PatternMode.Pattern);

    public const string ApiTokenUpdatePrefix = "api-token-update:";

    /// <summary>
    /// Fired when a login session is deleted (logout, password reset, deactivation). The payload is the id
    /// the session was stored under. Open connections that authenticated with it are closed.
    /// </summary>
    public static readonly RedisChannel SessionRevoked = new("session-revoked", RedisChannel.PatternMode.Literal);

    /// <summary>
    /// Fired when an account may no longer be used at all (deactivated or deleted). The payload is the user
    /// id. Every open connection of that user is closed, whatever it authenticated with.
    /// </summary>
    public static readonly RedisChannel UserAuthRevoked = new("user-auth-revoked", RedisChannel.PatternMode.Literal);

    /// <summary>
    /// Fired when a row is added to the email outbox, so the email consumer drains it immediately
    /// instead of waiting for its next poll. Payload-less: it is purely a "wake up and check" nudge.
    /// </summary>
    public static readonly RedisChannel EmailOutboxPending = new("email-outbox-pending", RedisChannel.PatternMode.Literal);
}
