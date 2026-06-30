namespace OpenShock.Cron.Services.Email.Outbox;

/// <summary>
/// The outbox's self-owned delivery policy: how many times a transiently-failing email is retried,
/// how long to wait before each retry, and how long a claimed ("Sending") row is leased before another
/// executor may reclaim it. All retry state lives on the row itself, so this is the only place the
/// curve is defined - there is no external job scheduler to keep in sync.
/// </summary>
public static class EmailOutboxRetryPolicy
{
    /// <summary>
    /// Maximum number of delivery attempts before a transiently-failing message is given up as
    /// <see cref="OpenShock.Common.Models.EmailStatus.Failed"/>. An attempt is counted when the row is
    /// claimed, so this also bounds crash/lease-expiry reclaim loops. Matches the prior Hangfire policy
    /// (one initial run plus ten retries).
    /// </summary>
    public const int MaxAttempts = 11;

    /// <summary>Delay before the first retry; each subsequent retry doubles it up to <see cref="MaxBackoff"/>.</summary>
    private static readonly TimeSpan BaseBackoff = TimeSpan.FromSeconds(30);

    /// <summary>Upper bound on the retry back-off, so a long outage doesn't push retries hours apart.</summary>
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromHours(1);

    /// <summary>
    /// How long a claimed row stays leased to its executor. If the executor doesn't reach a terminal
    /// state (or schedule the next retry) within this window - e.g. it crashed mid-send - the row
    /// becomes due again and another pass reclaims it. Comfortably longer than a healthy send.
    /// </summary>
    public static readonly TimeSpan DeliveryLease = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The back-off before retrying after <paramref name="attemptNumber"/> failed attempts (1-based):
    /// capped exponential growth (30s, 1m, 2m, 4m, ... up to 1h).
    /// </summary>
    public static TimeSpan BackoffFor(int attemptNumber)
    {
        if (attemptNumber < 1) attemptNumber = 1;

        // Cap the shift so 2^exponent can never overflow before the Min clamp.
        var exponent = Math.Min(attemptNumber - 1, 16);
        var scaled = BaseBackoff * Math.Pow(2, exponent);
        return scaled < MaxBackoff ? scaled : MaxBackoff;
    }
}
