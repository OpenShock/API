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
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<ClearOldEmailChangesJob> _logger;

    public ClearOldEmailChangesJob(OpenShockContext db, IHostApplicationLifetime lifetime, ILogger<ClearOldEmailChangesJob> logger)
    {
        _db = db;
        _lifetime = lifetime;
        _logger = logger;
    }

    public async Task<int> Execute()
    {
        var cancellationToken = _lifetime.ApplicationStopping;
        var expiredAtUtc = DateTime.UtcNow - Duration.EmailChangeRequestLifetime;
        var earliestCreatedOnUtc = expiredAtUtc - Duration.AuditRetentionTime;

        int nDeleted = await _db.UserEmailChanges
                                    .Where(x => x.UsedAt == null && x.CreatedAt < earliestCreatedOnUtc)
                                    .ExecuteDeleteAsync(cancellationToken);

        _logger.LogInformation("Deleted {deletedCount} expired email change requests since {earliestCreatedOnUtc}", nDeleted, earliestCreatedOnUtc);

        return nDeleted;
    }
}
