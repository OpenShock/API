using Microsoft.EntityFrameworkCore;
using OpenShock.Common;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Cron.Attributes;

namespace OpenShock.Cron.Jobs;

/// <summary>
/// Deletes old password requests if they have expired their lifetime and havent been used
/// </summary>
[CronJob("0 0 * * *")] // Every day at midnight (https://crontab.guru/)
public sealed class ClearOldPasswordResetsJob
{
    private readonly OpenShockContext _db;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="db"></param>
    public ClearOldPasswordResetsJob(OpenShockContext db)
    {
        _db = db;
    }

    public async Task Execute()
    {
        // Delete all password reset requests that have not been used and are older than the lifetime.
        // Leave expired requests that have been used for 14 days for moderation purposes.
        var earliestCreatedOn = DateTime.Now - (Duration.PasswordResetRequestLifetime + TimeSpan.FromDays(14));
        var earliestCreatedOnUtc = DateTime.SpecifyKind(earliestCreatedOn, DateTimeKind.Utc);

        // Run the delete query
        await _db.PasswordResets
            .Where(x => x.UsedOn == null && x.CreatedOn < earliestCreatedOnUtc)
            .ExecuteDeleteAsync();
    }
}