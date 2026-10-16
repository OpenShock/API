using Microsoft.EntityFrameworkCore;

namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// A pending increment for one <see cref="ControlStat"/> bucket.
/// </summary>
public readonly record struct ControlStatDelta(
    Guid OwnerUserId,
    Guid ShockerId,
    Guid? ControllerUserId,
    DateTime Bucket,
    ControlType Type,
    bool Live,
    long Count,
    long DurationMs,
    long IntensityMs,
    byte MaxIntensity)
{
    /// <summary>Granularity of <see cref="ControlStat.Bucket"/>, every time zone's UTC offset is a multiple of it.</summary>
    public static readonly TimeSpan BucketSize = TimeSpan.FromMinutes(15);

    /// <summary>Truncates a UTC timestamp to the start of its <see cref="BucketSize"/> bucket.</summary>
    public static DateTime ToBucket(DateTime utc) =>
        new(utc.Ticks - utc.Ticks % BucketSize.Ticks, DateTimeKind.Utc);

    /// <summary>Combines two deltas for the same bucket.</summary>
    public ControlStatDelta Merge(ControlStatDelta other) => this with
    {
        Count = Count + other.Count,
        DurationMs = DurationMs + other.DurationMs,
        IntensityMs = IntensityMs + other.IntensityMs,
        MaxIntensity = Math.Max(MaxIntensity, other.MaxIntensity)
    };
}

/// <summary>
/// Write path for the <c>control_stats</c> rollup table.
/// </summary>
public static class ControlStatQueries
{
    /// <summary>
    /// How long after its creation a buffered batch may still be applied, older ones are refused. This is what makes
    /// pruning <see cref="ControlStatBatch"/> markers safe: once no retry of a batch can be applied anymore, its marker
    /// has nothing left to deduplicate.
    /// </summary>
    public static readonly TimeSpan BatchReplayLifetime = TimeSpan.FromDays(30);

    /// <summary>
    /// Markers outlive <see cref="BatchReplayLifetime"/> by this much, covering clock skew between the services that
    /// create batches and the database that stamps the markers.
    /// </summary>
    private static readonly TimeSpan BatchMarkerMargin = TimeSpan.FromDays(1);

    /// <summary>
    /// Adds the given deltas onto their buckets in a single statement, creating buckets as needed. Deltas of shockers
    /// deleted in the meantime are kept with no shocker, like rows orphaned by the delete. Deltas of owners that no
    /// longer exist are dropped, their stats are gone with them.
    /// </summary>
    public static Task<int> UpsertControlStatsAsync(this OpenShockContext db, IEnumerable<ControlStatDelta> deltas,
        CancellationToken cancellationToken = default)
    {
        // One row per conflict key (ON CONFLICT can't touch a row twice in one statement), in a stable order so
        // concurrent upserts lock rows in the same order and can't deadlock.
        var merged = deltas
            .Where(d => d.Type != ControlType.Stop)
            .Select(d => d with { Bucket = ControlStatDelta.ToBucket(d.Bucket) })
            .GroupBy(d => (d.ShockerId, d.ControllerUserId, d.Bucket, d.Type, d.Live))
            .Select(g => g.Aggregate((a, b) => a.Merge(b)))
            .OrderBy(d => d.ShockerId)
            .ThenBy(d => d.ControllerUserId)
            .ThenBy(d => d.Bucket)
            .ThenBy(d => d.Type)
            .ThenBy(d => d.Live)
            .ToArray();

        if (merged.Length == 0) return Task.FromResult(0);

        var ids = merged.Select(_ => Guid.CreateVersion7()).ToArray();
        var ownerIds = merged.Select(d => d.OwnerUserId).ToArray();
        var shockerIds = merged.Select(d => d.ShockerId).ToArray();
        var controllerIds = merged.Select(d => d.ControllerUserId).ToArray();
        var buckets = merged.Select(d => d.Bucket).ToArray();
        var types = merged.Select(d => d.Type).ToArray();
        var lives = merged.Select(d => d.Live).ToArray();
        var counts = merged.Select(d => d.Count).ToArray();
        var durations = merged.Select(d => d.DurationMs).ToArray();
        var intensityMs = merged.Select(d => d.IntensityMs).ToArray();
        var maxIntensities = merged.Select(d => (short)d.MaxIntensity).ToArray();

        // A deleted shocker resolves to a null shocker_id, which falls outside the upsert key's filter and is
        // inserted as its own row instead of violating the foreign key.
        return db.Database.ExecuteSqlAsync(
            $"""
             INSERT INTO control_stats (id, owner_user_id, shocker_id, controller_user_id, bucket, type, live, count, duration_ms, intensity_ms, max_intensity, updated_at)
             SELECT v.id, v.owner_user_id, s.id, v.controller_user_id, v.bucket, v.type, v.live, v.count, v.duration_ms, v.intensity_ms, v.max_intensity, now()
             FROM unnest({ids}, {ownerIds}, {shockerIds}, {controllerIds}, {buckets}, {types}, {lives}, {counts}, {durations}, {intensityMs}, {maxIntensities})
               AS v(id, owner_user_id, shocker_id, controller_user_id, bucket, type, live, count, duration_ms, intensity_ms, max_intensity)
             JOIN users u ON u.id = v.owner_user_id
             LEFT JOIN shockers s ON s.id = v.shocker_id
             ON CONFLICT (owner_user_id, shocker_id, controller_user_id, bucket, type, live) WHERE shocker_id IS NOT NULL
             DO UPDATE SET
               count = control_stats.count + excluded.count,
               duration_ms = control_stats.duration_ms + excluded.duration_ms,
               intensity_ms = control_stats.intensity_ms + excluded.intensity_ms,
               max_intensity = GREATEST(control_stats.max_intensity, excluded.max_intensity),
               updated_at = excluded.updated_at
             """, cancellationToken);
    }

    /// <summary>
    /// Applies a buffered batch of deltas exactly once. The batch is marked applied in the same transaction, so
    /// retrying it with the same <paramref name="batchId"/> after an unknown commit outcome can't count it twice.
    /// Batches older than <see cref="BatchReplayLifetime"/> are refused, as their marker may have been pruned.
    /// </summary>
    public static async Task<ControlStatBatchResult> ApplyControlStatBatchAsync(this OpenShockContext db, Guid batchId,
        DateTime createdAt, IEnumerable<ControlStatDelta> deltas, CancellationToken cancellationToken = default)
    {
        if (DateTime.UtcNow - createdAt > BatchReplayLifetime) return ControlStatBatchResult.Expired;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var marked = await db.Database.ExecuteSqlAsync(
            $"INSERT INTO control_stat_batches (id, applied_at) VALUES ({batchId}, now()) ON CONFLICT DO NOTHING",
            cancellationToken);
        if (marked == 0) return ControlStatBatchResult.AlreadyApplied;

        await db.UpsertControlStatsAsync(deltas, cancellationToken);

        // Only markers of batches that can no longer be applied, see BatchReplayLifetime
        var prunableBefore = DateTime.UtcNow - BatchReplayLifetime - BatchMarkerMargin;
        await db.ControlStatBatches.Where(x => x.AppliedAt < prunableBefore).ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return ControlStatBatchResult.Applied;
    }
}

/// <summary>
/// Outcome of <see cref="ControlStatQueries.ApplyControlStatBatchAsync"/>.
/// </summary>
public enum ControlStatBatchResult
{
    Applied,

    /// <summary>A previous attempt with the same batch id was committed.</summary>
    AlreadyApplied,

    /// <summary>Older than <see cref="ControlStatQueries.BatchReplayLifetime"/>, not applied.</summary>
    Expired
}
