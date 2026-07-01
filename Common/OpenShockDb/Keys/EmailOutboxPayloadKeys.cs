
namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// Well-known keys for the dynamic <see cref="EmailOutboxMessage.Payload"/> dictionary. The payload
/// is intentionally an open key/value bag (stored as Postgres <c>jsonb</c>) so new email types can
/// carry whatever references they need without a schema change; these constants just keep the
/// well-known keys typo-safe. Values are always non-secret references (ids / addresses) - never a
/// rendered body and never a usable token.
/// </summary>
public static class EmailOutboxPayloadKeys
{
    /// <summary>Target user id. Used by <see cref="EmailType.AccountActivation"/>.</summary>
    public const string UserId = "userId";

    /// <summary><see cref="UserPasswordReset"/> id. Used by <see cref="EmailType.PasswordReset"/>.</summary>
    public const string PasswordResetId = "passwordResetId";

    /// <summary><see cref="UserEmailChange"/> id. Used by <see cref="EmailType.EmailVerification"/>.</summary>
    public const string EmailChangeId = "emailChangeId";

    /// <summary>The newly requested address. Used by <see cref="EmailType.EmailChangeNotice"/>.</summary>
    public const string NewEmail = "newEmail";
}
