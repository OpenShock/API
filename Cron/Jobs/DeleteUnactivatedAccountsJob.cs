using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Cron.Attributes;

namespace OpenShock.Cron.Jobs;

/// <summary>
/// Hard-deletes accounts that were never activated within <see cref="Duration.UnactivatedAccountRetentionTime"/>
/// of being created. Anyone can sign up with somebody else's address, and such an account would otherwise hold
/// that email and username forever and keep its activation link alive indefinitely. Deleting the account takes
/// its activation request with it, so this is also what expires the link.
/// </summary>
/// <remarks>
/// An unactivated account has never had a session, so it owns nothing worth keeping. Deactivated accounts are
/// left to <see cref="DeleteDeactivatedAccounts"/> (a moderation record), and privileged accounts are never touched.
/// </remarks>
[CronJob("30 * * * *")] // Every hour
public sealed class DeleteUnactivatedAccountsJob
{
    private readonly OpenShockContext _db;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<DeleteUnactivatedAccountsJob> _logger;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="db"/>
    /// <param name="lifetime">Its stopping token cancels the job when the server shuts down.</param>
    /// <param name="logger"/>
    public DeleteUnactivatedAccountsJob(OpenShockContext db, IHostApplicationLifetime lifetime, ILogger<DeleteUnactivatedAccountsJob> logger)
    {
        _db = db;
        _lifetime = lifetime;
        _logger = logger;
    }

    public async Task<int> Execute()
    {
        var cancellationToken = _lifetime.ApplicationStopping;
        var createdBefore = DateTime.UtcNow - Duration.UnactivatedAccountRetentionTime;

        // One statement, so an account activated between a read and the delete can't be caught:
        // Postgres rechecks a row that changed under the delete.
        var nDeleted = await _db.Database.ExecuteSqlAsync($"""
            DELETE FROM users AS u
            WHERE u.activated_at IS NULL
              AND u.created_at < {createdBefore}
              AND NOT (u.roles && {PrivilegedRoles.All})
              AND NOT EXISTS (SELECT 1 FROM user_deactivations AS d WHERE d.deactivated_user_id = u.id)
            """, cancellationToken);

        _logger.LogInformation("Deleted {DeletedCount} accounts that were never activated since {CreatedBefore}", nDeleted, createdBefore);

        return nDeleted;
    }
}
