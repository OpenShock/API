namespace OpenShock.Common.Options;

/// <summary>
/// Tuning for the email retry queue worker. Bound from <c>OpenShock:Mail:Queue</c>; every value has a
/// sensible default so the section can be omitted entirely.
/// </summary>
public sealed class EmailQueueOptions
{
    public const string SectionName = "OpenShock:Mail:Queue";

    /// <summary>How often the worker scans for due emails.</summary>
    public int PollIntervalSeconds { get; init; } = 30;

    /// <summary>Maximum number of due emails processed per scan.</summary>
    public int BatchSize { get; init; } = 50;

    /// <summary>Number of attempts before a queued email is given up on and dropped.</summary>
    public int MaxAttempts { get; init; } = 10;

    /// <summary>Base delay for the exponential backoff between attempts.</summary>
    public int BaseRetryDelaySeconds { get; init; } = 60;

    /// <summary>Upper bound on the backoff delay between attempts.</summary>
    public int MaxRetryDelaySeconds { get; init; } = 3600;

    /// <summary>
    /// Exponential backoff for the next attempt, capped at <see cref="MaxRetryDelaySeconds"/>.
    /// <paramref name="attempts"/> is the number of attempts already made (1 after the first failure).
    /// </summary>
    public TimeSpan GetRetryDelay(int attempts)
    {
        var exponent = Math.Max(0, attempts - 1);
        // Cap the exponent before shifting to avoid overflow on pathological attempt counts.
        var seconds = exponent >= 20
            ? MaxRetryDelaySeconds
            : Math.Min((double)MaxRetryDelaySeconds, BaseRetryDelaySeconds * Math.Pow(2, exponent));

        return TimeSpan.FromSeconds(seconds);
    }
}
