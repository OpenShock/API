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
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<ClearOldPasswordResetsJob> _logger;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="db"/>
    /// <param name="lifetime">Its stopping token cancels the job when the server shuts down.</param>
    /// <param name="logger"/>
    public ClearOldPasswordResetsJob(OpenShockContext db, IHostApplicationLifetime lifetime, ILogger<ClearOldPasswordResetsJob> logger)
    {
        _db = db;
        _lifetime = lifetime;
        _logger = logger;
    }

    public async Task<int> Execute()
    {
        var cancellationToken = _lifetime.ApplicationStopping;
        var expiredAtUtc = DateTime.UtcNow - Duration.PasswordResetRequestLifetime;
        var earliestCreatedOnUtc = expiredAtUtc - Duration.AuditRetentionTime;

        // Run the delete query
        int nDeleted = await _db.UserPasswordResets
                                    .Where(x => x.UsedAt == null && x.CreatedAt < earliestCreatedOnUtc)
                                    .ExecuteDeleteAsync(cancellationToken);
        
        _logger.LogInformation("Deleted {deletedCount} expired password resets since {earliestCreatedOnUtc}", nDeleted, earliestCreatedOnUtc);

        return nDeleted;
    }
}