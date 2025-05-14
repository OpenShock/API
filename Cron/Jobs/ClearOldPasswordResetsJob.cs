using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Cron.Attributes;

namespace OpenShock.Cron.Jobs;

/// <summary>
/// Deletes old password requests if they have expired their lifetime and haven't been used
/// </summary>
[CronJob("0 0 * * *")] // Every day at midnight (https://crontab.guru/)
public sealed class ClearOldPasswordResetsJob
{
    private readonly OpenShockContext _db;
    private readonly ILogger<ClearOldPasswordResetsJob> _logger;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="db"/>
    /// <param name="logger"/>
    public ClearOldPasswordResetsJob(OpenShockContext db, ILogger<ClearOldPasswordResetsJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Execute()
    {
        var expiredAtUtc = DateTime.UtcNow - Duration.PasswordResetRequestLifetime;
        var earliestCreatedOnUtc = expiredAtUtc - Duration.AuditRetentionTime;

        // Run the delete query
        int nDeleted = await _db.UserPasswordResets
                                    .Where(x => x.UsedAt == null && x.CreatedAt < earliestCreatedOnUtc)
                                    .ExecuteDeleteAsync();
        
        _logger.LogInformation("Deleted {deletedCount} expired password resets since {earliestCreatedOnUtc}", nDeleted, earliestCreatedOnUtc);
    }
}