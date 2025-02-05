namespace OpenShock.Common.Constants;

public static class Duration
{
    public static readonly TimeSpan AuditRetentionTime = TimeSpan.FromDays(90);
    public static readonly TimeSpan ShockerControlLogRetentionTime = TimeSpan.FromDays(365);
    
    public static readonly TimeSpan PasswordResetRequestLifetime = TimeSpan.FromHours(1);

    public static readonly TimeSpan NameChangeCooldown = TimeSpan.FromDays(7);
    
    public static readonly TimeSpan LoginSessionLifetime = TimeSpan.FromDays(30);
    public static readonly TimeSpan LoginSessionExpansionAfter = TimeSpan.FromDays(1);
    
    public static readonly TimeSpan DevicePingInitialDelay = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan DevicePingPeriod = TimeSpan.FromSeconds(15);
    public static readonly TimeSpan DeviceKeepAliveInitialTimeout = TimeSpan.FromSeconds(65);
    public static readonly TimeSpan DeviceKeepAliveTimeout = TimeSpan.FromSeconds(35);
}
