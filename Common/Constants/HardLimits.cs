namespace OpenShock.Common.Constants;

public static class HardLimits
{
    public const byte MinControlIntensity = 0;
    public const byte MaxControlIntensity = 100;

    public const ushort MinControlDuration = 300;
    public const ushort MaxControlDuration = 30000; // TODO: No reason to hard limit this to 30 seconds, can we extend it to ushort.MaxValue (65535)?
    
    public const int UsernameMinLength = 3;
    public const int UsernameMaxLength = 32;

    public const int EmailAddressMinLength = 5; // "a@b.c" (5 chars)
    public const int EmailAddressMaxLength = 320; // 64 + 1 + 255 (RFC 2821)

    public const int PasswordMinLength = 12;
    public const int PasswordMaxLength = 256;

    public const int UserAgentMaxLength = 1024;
    
    public const int ApiKeyNameMaxLength = 64;
    public const int ApiKeyTokenMinLength = 1;
    public const int ApiKeyTokenMaxLength = 64;
    public const int ApiKeyMaxPermissions = 256;
    
    public const int HubNameMinLength = 1;
    public const int HubNameMaxLength = 64;
    public const int HubTokenMaxLength = 256;
    
    public const int ShockerNameMinLength = 1;
    public const int ShockerNameMaxLength = 64;
    
    public const int ShockerShareLinkNameMinLength = 1;
    public const int ShockerShareLinkNameMaxLength = 64;

    public const int IpAddressMaxLength = 40;

    public const int SemVerMaxLength = 64;
    public const int OtaUpdateMessageMaxLength = 128;

    public const int PasswordHashMaxLength = 100;

    public const int UserEmailChangeSecretMaxLength = 128;
    public const int UserActivationSecretMaxLength = 128;
    public const int PasswordResetSecretMaxLength = 100;
    public const int ShockerControlLogCustomNameMaxLength = 64;
    
    public const int CreateShareRequestMaxShockers = 128;
}
