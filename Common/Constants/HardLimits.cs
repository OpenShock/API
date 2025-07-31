namespace OpenShock.Common.Constants;

public static class HardLimits
{
    public const byte MinControlIntensity = 0;
    public const byte MaxControlIntensity = 100;

    public const ushort MinControlDuration = 300;
    public const ushort MaxControlDuration = ushort.MaxValue; // 65.535 seconds
    
    public const int UsernameMinLength = 3;
    public const int UsernameMaxLength = 32;

    public const int EmailAddressMinLength = 5; // "a@b.c" (5 chars)
    public const int EmailAddressMaxLength = 320; // 64 + 1 + 255 (RFC 2821)
    public const int EmailProviderDomainMaxLength = 255;

    public const int PasswordMinLength = 12;
    public const int PasswordMaxLength = 256;

    public const int UserAgentMaxLength = 1024;
    
    public const int ApiKeyNameMaxLength = 64;
    public const int ApiKeyMaxPermissions = 256;
    
    public const int HubNameMinLength = 1;
    public const int HubNameMaxLength = 64;
    public const int HubTokenMaxLength = 256;
    
    public const int ShockerNameMinLength = 1;
    public const int ShockerNameMaxLength = 64;
    
    public const int PublicShareNameMinLength = 1;
    public const int PublicShareNameMaxLength = 64;

    public const int SemVerMaxLength = 64;
    public const int IpAddressMaxLength = 40;
    public const int Sha256HashHexLength = 64;

    public const int OtaUpdateMessageMaxLength = 128;

    public const int PasswordHashMaxLength = 100;

    public const int UserEmailChangeSecretMaxLength = 128;
    public const int UserActivationRequestSecretMaxLength = 128;
    public const int PasswordResetSecretMaxLength = 100;
    public const int ShockerControlLogCustomNameMaxLength = 64;
    
    public const int CreateShareRequestMaxShockers = 128;

    public const int MaxHubsPerUser = 4;
    public const int MaxShockersPerHub = 11;
    public const int MaxShockerControlLogsPerUser = 2048;
    
    // Don't allow any firmware prior to 2024.
    // Ridiculous edgecase: environment reports year at or prior to 2024, revert to 10 year limit just to be on the safe side
    public static readonly TimeSpan FirmwareMaxUptime = DateTime.UtcNow.Year <= 2024 ?
        TimeSpan.FromDays(365 * 10) :
        DateTime.UtcNow - new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}
