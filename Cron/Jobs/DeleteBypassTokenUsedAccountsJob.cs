using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Cron.Attributes;

namespace OpenShock.Cron.Jobs;

/// <summary>
/// Hard-deletes user accounts that authenticated through an admin-issued bypass token
/// whose owner enabled auto-cleanup, once the configured grace period has elapsed since
/// the last bypass use. Defends against leaked bypass tokens by ensuring test-style
/// accounts created through them do not persist indefinitely.
/// </summary>
[CronJob("0 * * * *")] // Every hour
public sealed class DeleteBypassTokenUsedAccountsJob
{
    private readonly OpenShockContext _db;
    private readonly ILogger<DeleteBypassTokenUsedAccountsJob> _logger;

    public DeleteBypassTokenUsedAccountsJob(OpenShockContext db, ILogger<DeleteBypassTokenUsedAccountsJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<int> Execute()
    {
        var now = DateTime.UtcNow;

        var userIds = await _db.BypassTokenUserUses
            .Where(use => use.BypassToken.AutoCleanupUsers
                          && use.BypassToken.AutoCleanupAfter != null
                          && use.LastUsedAt + use.BypassToken.AutoCleanupAfter < now)
            .Select(use => use.UserId)
            .Distinct()
            .ToListAsync();

        if (userIds.Count == 0)
        {
            _logger.LogDebug("No bypass-token-used accounts eligible for cleanup");
            return 0;
        }

        int nDeleted = await _db.Users
            .Where(u => userIds.Contains(u.Id) && !u.Roles.Contains(RoleType.Admin))
            .ExecuteDeleteAsync();

        _logger.LogInformation(
            "Bypass-token cleanup: {DeletedCount}/{CandidateCount} accounts deleted",
            nDeleted,
            userIds.Count);

        return nDeleted;
    }
}
