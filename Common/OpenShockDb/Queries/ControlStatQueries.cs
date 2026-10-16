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
    /// <summary>Truncates a UTC timestamp to the start of its hour, the granularity of <see cref="ControlStat.Bucket"/>.</summary>
    public static DateTime ToBucket(DateTime utc) =>
        new(utc.Ticks - utc.Ticks % TimeSpan.TicksPerHour, DateTimeKind.Utc);

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
    /// How long <see cref="ControlStatBatch"/> markers are kept. Batches are retried every flush, so a marker only has
    /// to outlive the retries of a batch whose commit acknowledgement got lost.
    /// </summary>
    private static readonly TimeSpan BatchMarkerRetention = TimeSpan.FromDays(1);

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
    /// </summary>
    /// <returns>False if the batch had already been applied.</returns>
    public static async Task<bool> ApplyControlStatBatchAsync(this OpenShockContext db, Guid batchId,
        IEnumerable<ControlStatDelta> deltas, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var marked = await db.Database.ExecuteSqlAsync(
            $"INSERT INTO control_stat_batches (id, applied_at) VALUES ({batchId}, now()) ON CONFLICT DO NOTHING",
            cancellationToken);
        if (marked == 0) return false;

        await db.UpsertControlStatsAsync(deltas, cancellationToken);

        var expiredBefore = DateTime.UtcNow - BatchMarkerRetention;
        await db.ControlStatBatches.Where(x => x.AppliedAt < expiredBefore).ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
