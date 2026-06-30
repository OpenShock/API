namespace OpenShock.Common.Constants;

public static class Duration
{
    public static readonly TimeSpan AuditRetentionTime = TimeSpan.FromDays(90);
    public static readonly TimeSpan ShockerControlLogRetentionTime = TimeSpan.FromDays(365);
    
    public static readonly TimeSpan PasswordResetRequestLifetime = TimeSpan.FromHours(1);

    public static readonly TimeSpan EmailChangeRequestLifetime = TimeSpan.FromHours(1);

    public static readonly TimeSpan NameChangeCooldown = TimeSpan.FromDays(7);
    
    public static readonly TimeSpan LoginSessionLifetime = TimeSpan.FromDays(30);
    public static readonly TimeSpan LoginSessionExpansionAfter = TimeSpan.FromDays(1);
    
    public static readonly TimeSpan DevicePingInitialDelay = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan DevicePingPeriod = TimeSpan.FromSeconds(15);
    public static readonly TimeSpan DeviceKeepAliveInitialTimeout = TimeSpan.FromSeconds(65);
    public static readonly TimeSpan DeviceKeepAliveTimeout = TimeSpan.FromSeconds(35);

    public static readonly TimeSpan DeactivatedAccountRetentionTime = TimeSpan.FromDays(14);

    /// <summary>Safety-net sweep interval for the email-outbox consumer. Normal hand-off is push-driven
    /// (Redis notification on enqueue); this only catches a missed notification. Hangfire owns retry
    /// timing for messages already handed off, so this need not be tight.</summary>
    public static readonly TimeSpan EmailOutboxPollInterval = TimeSpan.FromMinutes(1);
}
