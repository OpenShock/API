using OpenShock.Internal.Common.Constants;

namespace OpenShock.Common.Constants;

/// <summary>
/// Backend-API-specific hard limits. Shared limits live in
/// <see cref="OpenShock.Internal.Common.Constants.HardLimits"/> (OpenShock.Internal.Common package).
/// </summary>
public static class ApiHardLimits
{
    public const int EmailProviderDomainMaxLength = 255;

    public const int ApiKeyNameMaxLength = 64;
    public const int ApiKeyMaxPermissions = 256;

    public const int HubTokenMaxLength = 256;

    public const int OtaUpdateMessageMaxLength = 128;

    public const int PasswordHashMaxLength = 100;

    public const int EmailOutboxLastErrorMaxLength = 1024;
    public const int EmailOutboxCoalesceKeyMaxLength = 128;

    public const int UserEmailChangeSecretMaxLength = 128;
    public const int UserActivationRequestSecretMaxLength = 128;
    public const int PasswordResetSecretMaxLength = 100;
    public const int AuditReasonMaxLength = 512;

    public const int MaxTurnstileResponseTokenLength = 2048; // https://developers.cloudflare.com/turnstile/get-started/server-side-validation/

    // Don't allow any firmware prior to 2024.
    // Ridiculous edgecase: environment reports year at or prior to 2024, revert to 10 year limit just to be on the safe side
    public static readonly TimeSpan FirmwareMaxUptime = DateTime.UtcNow.Year <= 2024 ?
        TimeSpan.FromDays(365 * 10) :
        DateTime.UtcNow - new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}
