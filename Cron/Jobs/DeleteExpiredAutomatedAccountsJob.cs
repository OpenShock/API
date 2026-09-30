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

        // The same roles AccountService.DeleteAccountAsync treats as privileged.
        var candidates = await _db.Users
            .Where(u => u.CreatedByAutomationToken != null
                        && u.CreatedByAutomationToken.AutoCleanupUsers
                        && u.CreatedByAutomationToken.AutoCleanupAfter != null
                        && u.CreatedAt + u.CreatedByAutomationToken.AutoCleanupAfter < now
                        && !u.Roles.Any(r => r == RoleType.Staff || r == RoleType.Admin || r == RoleType.System))
            .Select(u => new { u.Id, u.CreatedByAutomationTokenId })
            .ToListAsync();

        if (candidates.Count == 0)
        {
            _logger.LogDebug("No automation-token-created accounts eligible for cleanup");
            return 0;
        }

        var userIds = candidates.Select(c => c.Id).ToArray();

        // Re-checked in the delete itself, in case an account was promoted since it was selected.
        int nDeleted = await _db.Users
            .Where(u => userIds.Contains(u.Id) && u.CreatedByAutomationTokenId != null && !u.Roles.Any(r => r == RoleType.Staff || r == RoleType.Admin || r == RoleType.System))
            .ExecuteDeleteAsync();

        // A user's audit log is deleted with the user, so the log line is the lasting record of what went.
        foreach (var candidate in candidates)
        {
            _logger.LogInformation(
                "Automation-token cleanup deleted account {UserId} created by automation token {AutomationTokenId}",
                candidate.Id, candidate.CreatedByAutomationTokenId);
        }

        _logger.LogInformation(
            "Automation-token cleanup: {DeletedCount}/{CandidateCount} accounts deleted",
            nDeleted,
            userIds.Length);

        return nDeleted;
    }
}
