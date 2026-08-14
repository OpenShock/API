using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace OpenShock.Common.HealthChecks;

/// <summary>
/// Decorates the built-in <see cref="HealthCheckService"/> and reuses the last report for a short
/// window instead of probing the dependencies on every call. Container and load balancer probes hit
/// the health endpoint every couple of seconds; without this, each one opens a fresh Postgres
/// connection and round-trips a Redis PING, so the cost scales with the number of probers rather
/// than with the thing being measured.
/// <para>
/// It decorates the service rather than the endpoint so the LCG's keep alive gate goes through the
/// same cache: a probe and the gateway then agree on what "ready" means at any instant, instead of
/// reading two independently-timed samples.
/// </para>
/// </summary>
public sealed class CachedHealthCheckService : HealthCheckService, IDisposable
{
    private readonly HealthCheckService _inner;
    private readonly IOptions<HealthCheckServiceOptions> _options;
    private readonly TimeSpan _ttl;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private volatile CacheEntry? _cache;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="inner">The real health check service this one delegates to on a miss.</param>
    /// <param name="options">Registrations, needed to resolve a caller's predicate against the cached report.</param>
    /// <param name="ttl">How long a report stays servable before the checks are actually re-run.</param>
    public CachedHealthCheckService(HealthCheckService inner, IOptions<HealthCheckServiceOptions> options, TimeSpan ttl)
    {
        _inner = inner;
        _options = options;
        _ttl = ttl;
    }

    /// <inheritdoc />
    public override async Task<HealthReport> CheckHealthAsync(Func<HealthCheckRegistration, bool>? predicate,
        CancellationToken cancellationToken = default)
    {
        var report = await GetOrRefreshAsync(cancellationToken);

        return Filter(report, predicate);
    }

    private async ValueTask<HealthReport> GetOrRefreshAsync(CancellationToken cancellationToken)
    {
        if (TryGetFresh(out var cached)) return cached;

        // One prober refreshes, the rest wait for its result. Without this, a burst of simultaneous
        // probes on a cold cache would each run the full set - exactly the load this class exists to avoid.
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (TryGetFresh(out cached)) return cached;

            // Timestamp the start, not the finish: the age of a report is how stale its observations
            // are, and a caller polling on the same interval as the TTL must not be handed back its
            // own previous answer just because producing it took a moment.
            var startedAt = Stopwatch.GetTimestamp();

            // Always run every registered check, never just the caller's subset. The predicate is a
            // delegate, so it cannot key a cache, but a full report can answer any subset by filtering.
            var report = await _inner.CheckHealthAsync(null, cancellationToken);

            _cache = new CacheEntry(report, startedAt);

            return report;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private bool TryGetFresh(out HealthReport report)
    {
        var entry = _cache;
        if (entry is not null && Stopwatch.GetElapsedTime(entry.TimestampTicks) < _ttl)
        {
            report = entry.Report;
            return true;
        }

        report = null!;
        return false;
    }

    /// <summary>
    /// Narrows a full report to the checks the caller asked for. <see cref="HealthReport.TotalDuration"/>
    /// stays the duration of the whole run, since that is what was actually spent producing these entries.
    /// </summary>
    private HealthReport Filter(HealthReport report, Func<HealthCheckRegistration, bool>? predicate)
    {
        if (predicate is null) return report;

        var selected = _options.Value.Registrations
            .Where(predicate)
            .Select(registration => registration.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (selected.Count == report.Entries.Count && report.Entries.Keys.All(selected.Contains)) return report;

        var entries = report.Entries
            .Where(entry => selected.Contains(entry.Key))
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);

        // This overload derives the aggregate status from the entries it is given, so dropping a
        // failing check that the caller did not select also drops it from the reported status.
        return new HealthReport(entries, report.TotalDuration);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _refreshLock.Dispose();

        // Not actually injected: the registration constructs the inner service itself, so the container
        // never tracks it and nothing else would ever dispose it. The built-in implementation holds
        // nothing disposable today, this only keeps that from becoming a leak if that changes.
#pragma warning disable IDISP007
        (_inner as IDisposable)?.Dispose();
#pragma warning restore IDISP007
    }

    private sealed record CacheEntry(HealthReport Report, long TimestampTicks);
}