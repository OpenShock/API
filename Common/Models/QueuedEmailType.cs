using NpgsqlTypes;

namespace OpenShock.Common.Models;

/// <summary>
/// Discriminates the kind of email a <see cref="OpenShock.Common.OpenShockDb.QueuedEmail"/> represents.
/// The row's JSONB payload shape depends on this value.
/// </summary>
public enum QueuedEmailType
{
    [PgName("account_activation")] AccountActivation = 0,
    [PgName("email_verification")] EmailVerification = 1,
    [PgName("email_change_notice")] EmailChangeNotice = 2,
}
