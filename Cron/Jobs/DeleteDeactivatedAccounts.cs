using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Cron.Attributes;

namespace OpenShock.Cron.Jobs;

/// <summary>
/// Deletes deactivated accounts after a set amount of time
/// </summary>
[CronJob("0 0 * * *")] // Every day at midnight (https://crontab.guru/)
public sealed class DeleteDeactivatedAccounts
{
    private readonly OpenShockContext _db;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<DeleteDeactivatedAccounts> _logger;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="db"/>
    /// <param name="lifetime">Its stopping token cancels the job when the server shuts down.</param>
    /// <param name="logger"/>
    public DeleteDeactivatedAccounts(OpenShockContext db, IHostApplicationLifetime lifetime, ILogger<DeleteDeactivatedAccounts> logger)
    {
        _db = db;
        _lifetime = lifetime;
        _logger = logger;
    }

    public async Task<int> Execute()
    {
        var cancellationToken = _lifetime.ApplicationStopping;
        var earliestCreatedAtUtc = DateTime.UtcNow - Duration.DeactivatedAccountRetentionTime;

        // Run the delete query
        int nDeleted = await _db.Users
                                    .Where(x => x.UserDeactivation != null && x.UserDeactivation.DeleteLater && x.UserDeactivation.CreatedAt <= earliestCreatedAtUtc)
                                    .ExecuteDeleteAsync(cancellationToken);

        _logger.LogInformation("Deleted {deletedCount} deactivated accounts since {earliestCreatedOnUtc}", nDeleted, earliestCreatedAtUtc);

        return nDeleted;
    }
}