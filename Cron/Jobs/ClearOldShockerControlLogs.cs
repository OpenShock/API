using Microsoft.EntityFrameworkCore;
using OpenShock.Common;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Cron.Attributes;

namespace OpenShock.Cron.Jobs;

/// <summary>
/// Deletes shocker control logs older than 9 months
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
        var earliestCreatedAtUtc = DateTime.UtcNow - Duration.PasswordResetRequestLifetime;

        // Run the delete query
        int nDeleted = await _db.ShockerControlLogs
                                    .Where(x => x.CreatedOn < earliestCreatedAtUtc)
                                    .ExecuteDeleteAsync();
        
        _logger.LogInformation("Deleted {deletedCount} shocker control logs since {earliestCreatedOnUtc}", nDeleted, earliestCreatedAtUtc);
    }
}