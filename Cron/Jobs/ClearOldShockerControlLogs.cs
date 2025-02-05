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
    private const int MaxLogCount = 1_000_000;

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
        
        // Check the remaining number of logs to ensure it does not exceed the maximum allowed.
        var remainingLogsCount = await _db.ShockerControlLogs.CountAsync();
        
        if (remainingLogsCount > MaxLogCount)
        {
            _logger.LogInformation("Log count exceeds the maximum limit: {RemainingLogsCount} logs present, but the limit is {MaxLogCount}.", remainingLogsCount, MaxLogCount);
            
            // Identify the timestamp of the oldest log that should be retained to meet the log count limit.
            var oldestLogToKeep = await _db.ShockerControlLogs
                .OrderByDescending(log => log.CreatedOn)
                .Select(log => log.CreatedOn)
                .Skip(MaxLogCount)
                .FirstAsync();
            
            _logger.LogInformation("Preparing to delete logs created before {OldestLogToKeep:O} to enforce the log count limit.", oldestLogToKeep);

            // Delete logs that were created before the identified cutoff date.
            var deletedByCount = await _db.ShockerControlLogs
                .Where(log => log.CreatedOn < oldestLogToKeep)
                .ExecuteDeleteAsync();
            
            _logger.LogInformation("Deleted {DeletedByCount} additional logs older than {OldestLogToKeep:O}.", deletedByCount, oldestLogToKeep);
        }
    }
}