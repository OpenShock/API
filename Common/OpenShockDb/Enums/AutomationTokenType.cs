using NpgsqlTypes;
using OpenShock.Common.Utils;

namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// A protection that an automation token is allowed to switch off.
/// </summary>
[PgEnum(Name = "automation_token_type")]
public enum AutomationTokenType
{
    [PgName("turnstile")] Turnstile = 0,
    [PgName("rate_limit")] RateLimit = 1,
}
