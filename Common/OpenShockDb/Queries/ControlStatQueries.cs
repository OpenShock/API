using Microsoft.EntityFrameworkCore;

namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// A pending increment for one <see cref="ControlStat"/> bucket.
/// </summary>
public readonly record struct ControlStatDelta(
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
    /// Adds the given deltas onto their buckets in a single statement, creating buckets as needed. The owner is
    /// resolved through <c>shocker -&gt; device</c>, deltas for shockers that no longer exist are dropped.
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
        var shockerIds = merged.Select(d => d.ShockerId).ToArray();
        var controllerIds = merged.Select(d => d.ControllerUserId).ToArray();
        var buckets = merged.Select(d => d.Bucket).ToArray();
        var types = merged.Select(d => d.Type).ToArray();
        var lives = merged.Select(d => d.Live).ToArray();
        var counts = merged.Select(d => d.Count).ToArray();
        var durations = merged.Select(d => d.DurationMs).ToArray();
        var intensityMs = merged.Select(d => d.IntensityMs).ToArray();
        var maxIntensities = merged.Select(d => (short)d.MaxIntensity).ToArray();

        return db.Database.ExecuteSqlAsync(
            $"""
             INSERT INTO control_stats (id, owner_user_id, shocker_id, controller_user_id, bucket, type, live, count, duration_ms, intensity_ms, max_intensity)
             SELECT v.id, d.owner_id, v.shocker_id, v.controller_user_id, v.bucket, v.type, v.live, v.count, v.duration_ms, v.intensity_ms, v.max_intensity
             FROM unnest({ids}, {shockerIds}, {controllerIds}, {buckets}, {types}, {lives}, {counts}, {durations}, {intensityMs}, {maxIntensities})
               AS v(id, shocker_id, controller_user_id, bucket, type, live, count, duration_ms, intensity_ms, max_intensity)
             JOIN shockers s ON s.id = v.shocker_id
             JOIN devices d ON d.id = s.device_id
             ON CONFLICT (owner_user_id, shocker_id, controller_user_id, bucket, type, live) WHERE shocker_id IS NOT NULL
             DO UPDATE SET
               count = control_stats.count + excluded.count,
               duration_ms = control_stats.duration_ms + excluded.duration_ms,
               intensity_ms = control_stats.intensity_ms + excluded.intensity_ms,
               max_intensity = GREATEST(control_stats.max_intensity, excluded.max_intensity)
             """, cancellationToken);
    }
}
