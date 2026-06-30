using NpgsqlTypes;
using OpenShock.Common.Utils;

namespace OpenShock.Common.Models;

/// <summary>
/// The kind of email an <see cref="OpenShockDb.EmailOutboxMessage"/> represents. The outbox stores
/// only the <em>intent</em> to send one of these, never a rendered body or a usable secret, the
/// consumer maps the type to the matching template and (for token-bearing types) mints a fresh
/// secret at send time.
/// </summary>
[PgEnum]
public enum EmailType
{
    /// <summary>Account activation / email confirmation for a newly created account.</summary>
    [PgName("account_activation")] AccountActivation,

    /// <summary>Password reset link.</summary>
    [PgName("password_reset")] PasswordReset,

    /// <summary>Verification of a newly requested email address (sent to the new address).</summary>
    [PgName("email_verification")] EmailVerification,

    /// <summary>Informational notice sent to the previous address when an email change is requested. Carries no secret.</summary>
    [PgName("email_change_notice")] EmailChangeNotice
}
