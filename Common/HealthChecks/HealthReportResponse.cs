using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace OpenShock.Common.HealthChecks;

/// <summary>
/// Body written by the health endpoint. Reports the per-check breakdown rather than the bare status
/// word the default writer emits, so a failing probe names the dependency that is down.
/// </summary>
public sealed class HealthReportResponse
{
    /// <summary>Aggregate status: the worst individual result.</summary>
    public required HealthStatus Status { get; init; }

    /// <summary>How long the whole report took to produce.</summary>
    public required double DurationMs { get; init; }

    /// <summary>Individual results, keyed by the name the check is registered under.</summary>
    public required IReadOnlyDictionary<string, HealthReportEntryResponse> Entries { get; init; }

    /// <summary>
    /// Projects a <see cref="HealthReport"/> onto the wire shape.
    /// </summary>
    public static HealthReportResponse From(HealthReport report) => new()
    {
        Status = report.Status,
        DurationMs = report.TotalDuration.TotalMilliseconds,
        Entries = report.Entries.ToDictionary(
            entry => entry.Key,
            entry => new HealthReportEntryResponse
            {
                Status = entry.Value.Status,
                DurationMs = entry.Value.Duration.TotalMilliseconds,
                Description = entry.Value.Description,
                Error = entry.Value.Exception?.Message
            })
    };
}

/// <summary>
/// Result of a single health check.
/// </summary>
public sealed class HealthReportEntryResponse
{
    /// <summary>Status this check reported.</summary>
    public required HealthStatus Status { get; init; }

    /// <summary>How long this check took.</summary>
    public required double DurationMs { get; init; }

    /// <summary>Human readable detail, e.g. the measured Redis PING latency.</summary>
    public string? Description { get; init; }

    /// <summary>Message of the exception that failed the check, if any.</summary>
    public string? Error { get; init; }
}
