namespace OpenShock.Common.HealthChecks;

/// <summary>
/// Tags used to select subsets of the registered health checks.
/// </summary>
public static class HealthCheckTags
{
    /// <summary>
    /// A dependency the host needs before it can actually serve (Postgres, Redis). Tagging keeps the
    /// set explicit, so a check added later for something non-essential does not silently start
    /// de-onlining gateways.
    /// </summary>
    public const string Ready = "ready";
}

/// <summary>
/// Names the individual health checks are registered under. They show up verbatim in the
/// health payload and in the LCG's "not refreshing keep alive" log line.
/// </summary>
public static class HealthCheckNames
{
    /// <summary>Postgres reachability.</summary>
    public const string Database = "database";

    /// <summary>Redis reachability.</summary>
    public const string Redis = "redis";
}

/// <summary>
/// Path the health endpoint is served on.
/// </summary>
public static class HealthCheckPaths
{
    /// <summary>
    /// Runs every <see cref="HealthCheckTags.Ready"/> check and reports the breakdown. This is the
    /// same set the LCG gates its self-onlining on, so a probe sees exactly what the gateway acts on.
    /// </summary>
    public const string Health = "/1/health";
}
