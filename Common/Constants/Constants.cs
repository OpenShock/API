namespace OpenShock.Common.Constants;

public static class Duration
{
    public static readonly TimeSpan AuditRetentionTime = TimeSpan.FromDays(90);
    public static readonly TimeSpan ShockerControlLogRetentionTime = TimeSpan.FromDays(365);
    
    public static readonly TimeSpan PasswordResetRequestLifetime = TimeSpan.FromHours(1);

    public static readonly TimeSpan EmailChangeRequestLifetime = TimeSpan.FromHours(1);

    public static readonly TimeSpan ActivationResendCooldown = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan ActivationResendWindow = TimeSpan.FromHours(24);

    /// <summary>
    /// How long a never activated account keeps its address from a new signup that has not proven the
    /// address is theirs, counted from its creation and from its last activation email. Long enough for a
    /// delayed email to arrive and be opened, so someone who only knows the address cannot take a signup
    /// away from the person completing it.
    /// </summary>
    public static readonly TimeSpan PendingSignupHoldTime = TimeSpan.FromHours(1);
    public static readonly TimeSpan UnactivatedAccountRetentionTime = TimeSpan.FromDays(7);

    public static readonly TimeSpan NameChangeCooldown = TimeSpan.FromDays(7);
    
    public static readonly TimeSpan LoginSessionLifetime = TimeSpan.FromDays(30);
    public static readonly TimeSpan LoginSessionExpansionAfter = TimeSpan.FromDays(1);
    
    public static readonly TimeSpan DevicePingInitialDelay = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan DevicePingPeriod = TimeSpan.FromSeconds(15);
    public static readonly TimeSpan DeviceKeepAliveInitialTimeout = TimeSpan.FromSeconds(65);
    public static readonly TimeSpan DeviceKeepAliveTimeout = TimeSpan.FromSeconds(35);

    public static readonly TimeSpan HubControlMaxQueueAge = TimeSpan.FromSeconds(5);

    public static readonly TimeSpan DeactivatedAccountRetentionTime = TimeSpan.FromDays(14);
}
