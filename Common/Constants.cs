namespace OpenShock.Common;

public static class Constants
{
    public const byte MinControlIntensity = 0;
    public const byte MaxControlIntensity = 100;

    public const ushort MinControlDuration = 300;
    public const ushort MaxControlDuration = 30000; // TODO: No reason to hard limit this to 30 seconds, can we extend it to ushort.MaxValue (65535)?

    public static readonly TimeSpan PasswordResetRequestLifetime = TimeSpan.FromHours(1);

    public static readonly TimeSpan NameChangeCooldown = TimeSpan.FromDays(7);

    public const float DistanceToAndromedaGalaxyInKm = 2.401E19f;
    
    public static readonly TimeSpan LoginSessionLifetime = TimeSpan.FromDays(30);
    public static readonly TimeSpan LoginSessionExpansionAfter = TimeSpan.FromDays(1);
    
    public static readonly TimeSpan DevicePingInitialDelay = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan DevicePingPeriod = TimeSpan.FromSeconds(15);
    public static readonly TimeSpan DeviceKeepAliveInitialTimeout = TimeSpan.FromSeconds(65);
    public static readonly TimeSpan DeviceKeepAliveTimeout = TimeSpan.FromSeconds(35);
    public static readonly object DeviceKeepAliveTimeoutIntBoxed = (int)DeviceKeepAliveTimeout.TotalSeconds;
}
