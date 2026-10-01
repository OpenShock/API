using Microsoft.EntityFrameworkCore;
using OpenShock.Common.OpenShockDb;
using OpenShock.Cron.Attributes;

namespace OpenShock.Cron.Jobs;

/// <summary>
/// Hard-deletes automated accounts (mainly test accounts) that were created through an admin-issued
/// automation token whose owner enabled auto-cleanup (off by default), once the token's configured lifetime
/// has passed since the account was created. Accounts the token was merely used with are never touched,
/// only the ones it created.
/// </summary>
[CronJob("0 * * * *")] // Every hour
public sealed class DeleteExpiredAutomatedAccountsJob
{
    private readonly OpenShockContext _db;
    private readonly ILogger<DeleteExpiredAutomatedAccountsJob> _logger;

    public DeleteExpiredAutomatedAccountsJob(OpenShockContext db, ILogger<DeleteExpiredAutomatedAccountsJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<int> Execute()
    {
        var now = DateTime.UtcNow;

        // Selecting and deleting in one statement means an account promoted, or a token whose cleanup was switched
        // off, between a read and the delete can't slip through.
        var deleted = await _db.Database.SqlQuery<DeletedAccount>($"""
            DELETE FROM users AS u
            USING automation_tokens AS t
            WHERE t.id = u.created_by_automation_token_id
              AND t.auto_cleanup_users
              AND t.auto_cleanup_after IS NOT NULL
              AND u.created_at + t.auto_cleanup_after < {now}
              AND NOT (u.roles && {PrivilegedRoles.All})
            RETURNING u.id AS "UserId", u.created_by_automation_token_id AS "AutomationTokenId"
            """).ToListAsync();

        if (deleted.Count == 0)
        {
            _logger.LogDebug("No automation-token-created accounts eligible for cleanup");
            return 0;
        }

        // A user's audit log is deleted with the user, so the log line is the lasting record of what went.
        foreach (var account in deleted)
        {
            _logger.LogInformation(
                "Automation-token cleanup deleted account {UserId} created by automation token {AutomationTokenId}",
                account.UserId, account.AutomationTokenId);
        }

        _logger.LogInformation("Automation-token cleanup: {DeletedCount} accounts deleted", deleted.Count);

        return deleted.Count;
    }

    private sealed record DeletedAccount(Guid UserId, Guid AutomationTokenId);
}
