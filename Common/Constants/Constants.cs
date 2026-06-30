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

    /// <summary>How long a claimed email-outbox row may be in flight before it is treated as
    /// abandoned (worker crashed mid-send) and eligible to be re-claimed.</summary>
    public static readonly TimeSpan EmailOutboxLeaseTimeout = TimeSpan.FromMinutes(5);

    /// <summary>Safety-net poll interval for the email-outbox consumer. Normal delivery is push-driven
    /// (Redis notification on enqueue); this only catches due retries and any missed notification.</summary>
    public static readonly TimeSpan EmailOutboxPollInterval = TimeSpan.FromSeconds(15);

    /// <summary>Base delay for the first email-outbox retry; doubles each subsequent attempt.</summary>
    public static readonly TimeSpan EmailOutboxRetryBaseDelay = TimeSpan.FromSeconds(30);

    /// <summary>Upper bound on the exponential email-outbox retry back-off.</summary>
    public static readonly TimeSpan EmailOutboxRetryMaxDelay = TimeSpan.FromHours(2);
}
