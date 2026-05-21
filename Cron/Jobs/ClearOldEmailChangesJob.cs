using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Cron.Attributes;

namespace OpenShock.Cron.Jobs;

/// <summary>
/// Deletes unused email change requests after they have been expired for an additional audit-retention period.
/// </summary>
[CronJob("0 0 * * *")] // Every day at midnight (https://crontab.guru/)
public sealed class ClearOldEmailChangesJob
{
    private readonly OpenShockContext _db;
    private readonly ILogger<ClearOldEmailChangesJob> _logger;

    public ClearOldEmailChangesJob(OpenShockContext db, ILogger<ClearOldEmailChangesJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<int> Execute()
    {
        var expiredAtUtc = DateTime.UtcNow - Duration.EmailChangeRequestLifetime;
        var earliestCreatedOnUtc = expiredAtUtc - Duration.AuditRetentionTime;

        int nDeleted = await _db.UserEmailChanges
                                    .Where(x => x.UsedAt == null && x.CreatedAt < earliestCreatedOnUtc)
                                    .ExecuteDeleteAsync();

        _logger.LogInformation("Deleted {deletedCount} expired email change requests since {earliestCreatedOnUtc}", nDeleted, earliestCreatedOnUtc);

        return nDeleted;
    }
}
