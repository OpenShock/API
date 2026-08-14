using NpgsqlTypes;
using OpenShock.Common.Utils;

namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// The kind of email an <see cref="EmailOutboxMessage"/> represents. The outbox stores
/// only the <em>intent</em> to send one of these, never a rendered body or a usable secret, the
/// consumer maps the type to the matching template and (for token-bearing types) mints a fresh
/// secret at send time.
/// </summary>
[PgEnum(Name = "email_type")]
public enum EmailType
{
    /// <summary>Account activation / email confirmation for a newly created account.</summary>
    [PgName("account_activation")] AccountActivation = 0,

    /// <summary>Password reset link.</summary>
    [PgName("password_reset")] PasswordReset = 1,

    /// <summary>Verification of a newly requested email address (sent to the new address).</summary>
    [PgName("email_verification")] EmailVerification = 2,

    /// <summary>Informational notice sent to the previous address when an email change is requested. Carries no secret.</summary>
    [PgName("email_change_notice")] EmailChangeNotice = 3
}
