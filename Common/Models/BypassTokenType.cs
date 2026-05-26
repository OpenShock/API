using NpgsqlTypes;

namespace OpenShock.Common.Models;

public enum BypassTokenType
{
    [PgName("turnstile")] Turnstile,
    [PgName("rate_limit")] RateLimit
}
