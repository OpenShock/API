using Microsoft.EntityFrameworkCore;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.ControlStats;

namespace OpenShock.LiveControlGateway;

/// <summary>
/// Collects live control activity from all hub lifetimes on this gateway and periodically flushes it into the
/// <c>control_stats</c> rollup, so the per-tick hot path never touches the database.
/// </summary>
public sealed class LiveControlStatsService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    private readonly IDbContextFactory<OpenShockContext> _dbFactory;
    private readonly ILogger<LiveControlStatsService> _logger;
    private readonly ControlStatsAccumulator _pending = new();

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="dbFactory"></param>
    /// <param name="logger"></param>
    public LiveControlStatsService(IDbContextFactory<OpenShockContext> dbFactory, ILogger<LiveControlStatsService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <summary>
    /// Record live control activity
    /// </summary>
    /// <param name="delta"></param>
    public void Record(ControlStatDelta delta) => _pending.Add(delta);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await Flush();
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown requested, fall through to the final flush
        }

        await Flush();
    }

    private async Task Flush()
    {
        var deltas = _pending.DequeueAll();
        if (deltas.Length == 0) return;

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            await db.UpsertControlStatsAsync(deltas);
        }
        catch (Exception e)
        {
            // Unlike a log line these can't be rebuilt, so put them back for the next flush to retry
            foreach (var delta in deltas) _pending.Add(delta);
            _logger.LogError(e, "Failed to flush {Count} live control stat buckets", deltas.Length);
        }
    }
}
