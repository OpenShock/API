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

        var expired = await _db.Users
            .Where(u => u.CreatedByAutomationToken != null
                        && u.CreatedByAutomationToken.AutoCleanupUsers
                        && u.CreatedByAutomationToken.AutoCleanupAfter != null
                        && u.CreatedAt + u.CreatedByAutomationToken.AutoCleanupAfter < now)
            .Select(u => new { u.Id, u.CreatedByAutomationTokenId, u.Roles })
            .ToListAsync();

        var candidates = expired.Where(u => !PrivilegedRoles.Any(u.Roles)).ToList();
        if (candidates.Count == 0)
        {
            _logger.LogDebug("No automation-token-created accounts eligible for cleanup");
            return 0;
        }

        var candidateIds = candidates.Select(c => c.Id).ToArray();

        // The roles and the token's cleanup setting are checked again as the rows are deleted, in case an account
        // was promoted or its token's cleanup switched off since it was selected.
        await _db.Database.ExecuteSqlAsync($"""
            DELETE FROM users AS u
            USING automation_tokens AS t
            WHERE u.id = ANY({candidateIds})
              AND t.id = u.created_by_automation_token_id
              AND t.auto_cleanup_users
              AND NOT (u.roles && {PrivilegedRoles.All})
            """);

        // The delete can't say which rows it removed, so read back the candidates the re-check kept.
        var kept = await _db.Users
            .Where(u => candidateIds.Contains(u.Id))
            .Select(u => u.Id)
            .ToHashSetAsync();

        // A user's audit log is deleted with the user, so the log line is the lasting record of what went.
        var nDeleted = 0;
        foreach (var candidate in candidates)
        {
            if (kept.Contains(candidate.Id)) continue;

            nDeleted++;
            _logger.LogInformation(
                "Automation-token cleanup deleted account {UserId} created by automation token {AutomationTokenId}",
                candidate.Id, candidate.CreatedByAutomationTokenId);
        }

        _logger.LogInformation(
            "Automation-token cleanup: {DeletedCount}/{CandidateCount} accounts deleted",
            nDeleted,
            candidates.Count);

        return nDeleted;
    }
}
