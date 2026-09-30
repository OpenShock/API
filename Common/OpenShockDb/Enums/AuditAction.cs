using NpgsqlTypes;
using OpenShock.Common.Utils;

namespace OpenShock.Common.OpenShockDb;

[PgEnum(Name = "audit_action")]
public enum AuditAction
{
    [PgName("login")] Login,
    [PgName("logout")] Logout,
    [PgName("password_changed")] PasswordChanged,
    [PgName("email_change_requested")] EmailChangeRequested,
    [PgName("email_changed")] EmailChanged,
    [PgName("username_changed")] UsernameChanged,
    [PgName("api_token_created")] ApiTokenCreated,
    [PgName("api_token_deleted")] ApiTokenDeleted,
    [PgName("oauth_connected")] OAuthConnected,
    [PgName("oauth_disconnected")] OAuthDisconnected,
    [PgName("account_deactivated")] AccountDeactivated,
    [PgName("account_reactivated")] AccountReactivated,
    [PgName("account_deleted")] AccountDeleted,
}
