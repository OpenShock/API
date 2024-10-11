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
}
