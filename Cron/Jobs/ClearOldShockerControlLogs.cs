using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Cron.Attributes;

namespace OpenShock.Cron.Jobs;

/// <summary>
/// Deletes shocker control logs by enforcing a maximum log count per user
/// </summary>
[CronJob("0 0 * * *")] // Every day at midnight (https://crontab.guru/)
public sealed class ClearOldShockerControlLogs
{
    private readonly OpenShockContext _db;
    private readonly ILogger<ClearOldShockerControlLogs> _logger;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="db"></param>
    /// <param name="logger"></param>
    public ClearOldShockerControlLogs(OpenShockContext db, ILogger<ClearOldShockerControlLogs> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<int> Execute()
    {
        var deletedUserLimits = await _db.Database.ExecuteSqlAsync(
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
               AND rl.rn > {HardLimits.MaxShockerControlLogsPerUser}
             """);

        _logger.LogInformation("Deleted {deletedUserLimits} shocker control logs exceeding the per-user limit",
            deletedUserLimits);

        return deletedUserLimits;
    }
}