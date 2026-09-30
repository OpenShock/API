using NpgsqlTypes;
using OpenShock.Common.Utils;

namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// A protection that a bypass token is allowed to switch off.
/// </summary>
[PgEnum(Name = "bypass_token_type")]
public enum BypassTokenType
{
    [PgName("turnstile")] Turnstile = 0,
    [PgName("rate_limit")] RateLimit = 1,
}
