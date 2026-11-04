using NpgsqlTypes;
using OpenShock.Common.Utils;

namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// A protection that an automation token is allowed to switch off, or a service capability it grants.
/// </summary>
[PgEnum(Name = "automation_token_type")]
public enum AutomationTokenType
{
    [PgName("turnstile")] Turnstile = 0,
    [PgName("rate_limit")] RateLimit = 1,

    /// <summary>
    /// Read control statistics of arbitrary users through the integration endpoints, used by first party
    /// services like the Discord bot to keep profile widgets in sync.
    /// </summary>
    [PgName("control_stats_read")] ControlStatsRead = 2,
}
