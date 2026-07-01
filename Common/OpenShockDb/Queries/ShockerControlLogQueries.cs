using Microsoft.EntityFrameworkCore;

namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// Reusable commands over the <c>shocker_control_logs</c> table. Kept here (rather than inline in the
/// Cron cleanup job) so the raw SQL is a single source of truth that integration tests can execute
/// directly - a table or column rename then breaks the test rather than surfacing only in production.
/// </summary>
public static class ShockerControlLogQueries
{
    /// <summary>
    /// Retention trim: per owning user, keeps the newest <paramref name="maxPerUser"/> control logs (by
    /// <c>created_at</c>) and deletes the rest. Ownership is resolved through
    /// <c>shocker -&gt; device -&gt; owner</c>, so the cap is per user, not per shocker or device. Runs as a
    /// single set-based statement (a window function ranks each user's logs newest-first; anything past the
    /// cap is deleted). Returns the number of rows deleted.
    /// </summary>
    public static Task<int> DeleteControlLogsBeyondPerUserLimitAsync(this OpenShockContext db, int maxPerUser,
        CancellationToken cancellationToken = default)
    {
        if (maxPerUser < 0) throw new ArgumentOutOfRangeException(nameof(maxPerUser));

        return db.Database.ExecuteSqlAsync(
            $"""
             WITH ranked_logs AS (
               SELECT
                 l.id,
                 ROW_NUMBER() OVER (PARTITION BY d.owner_id ORDER BY l.created_at DESC) AS rn
               FROM shocker_control_logs l
               JOIN shockers s ON s.id = l.shocker_id
               JOIN devices d ON d.id = s.device_id
             )
             DELETE FROM shocker_control_logs l
             USING ranked_logs rl
             WHERE l.id = rl.id
               AND rl.rn > {maxPerUser}
             """, cancellationToken);
    }
}
