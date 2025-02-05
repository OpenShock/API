using Microsoft.EntityFrameworkCore;
using OpenShock.Common;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Cron.Attributes;

namespace OpenShock.Cron.Jobs;

/// <summary>
/// Deletes shocker control logs older than the retention period and enforces a maximum log count
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

    public async Task Execute()
    {
        // Calculate the retention threshold based on configured retention time.
        var retentionThreshold = DateTime.UtcNow - Duration.ShockerControlLogRetentionTime;

        // Delete logs older than the retention threshold.
        var deletedByAge = await _db.ShockerControlLogs
            .Where(log => log.CreatedOn < retentionThreshold)
            .ExecuteDeleteAsync();
        
        _logger.LogInformation("Deleted {deletedCount} shocker control logs older than {retentionThreshold:O}.", deletedByAge, retentionThreshold);
        
        var userLogsCounts = await _db.ShockerControlLogs
            .GroupBy(log => log.Shocker.DeviceNavigation.Owner)
            .Select(group => new
            {
                UserId = group.Key,
                CountToDelete = Math.Max(0, group.Count() - HardLimits.MaxShockerControlLogsPerUser),
                DeleteBeforeDate = group
                    .OrderByDescending(log => log.CreatedOn)
                    .Skip(HardLimits.MaxShockerControlLogsPerUser)
                    .Select(log => log.CreatedOn)
                    .FirstOrDefault()
            })
            .Where(result => result.CountToDelete > 0)
            .ToArrayAsync();

        if (userLogsCounts.Length != 0)
        {
            _logger.LogInformation("A total of {totalLogsToDelete} logs will be deleted to enforce per-user limits.", userLogsCounts.Sum(x => x.CountToDelete));
        
            foreach (var userLogCount in userLogsCounts)
            {
                await _db.ShockerControlLogs
                    .Where(log => log.Shocker.DeviceNavigation.Owner == userLogCount.UserId && log.CreatedOn < userLogCount.DeleteBeforeDate)
                    .ExecuteDeleteAsync();
            }
        }

        _logger.LogInformation("Done!");
    }
}