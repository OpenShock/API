namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// The account flow an automation token was used in, recorded on <see cref="AuditAction.AutomationTokenUsed"/> entries.
/// </summary>
public enum AutomationTokenFlow
{
    Signup,
    Login,
    PasswordReset,
    ReportTokens,
}
