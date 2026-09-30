using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.Audit;
using OpenShock.Common.Services.AutomationTokens;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Deletes an automation token together with every account it created. Refused while any of those
    /// accounts holds a privileged role. Other API instances may keep honoring the token for up to 30 seconds
    /// </summary>
    [HttpDelete("automationTokens/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAutomationToken(
        [FromRoute] Guid id,
        [FromServices] IAutomationTokenService automationTokens,
        [FromServices] IAuditService auditService,
        CancellationToken ct)
    {
        PedanticallyEnsureAdmin();

        var token = await _db.AutomationTokens.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (token is null) return NotFound();

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var createdUsers = await _db.Users
            .Where(u => u.CreatedByAutomationTokenId == id)
            .Select(u => new
            {
                u.Id,
                IsPrivileged = u.Roles.Any(r => r == RoleType.Staff || r == RoleType.Admin || r == RoleType.System)
            })
            .ToListAsync(ct);

        // The accounts go through the database's cascade, which can't skip privileged ones, so refuse instead.
        // The same roles AccountService.DeleteAccountAsync treats as privileged.
        if (createdUsers.Any(u => u.IsPrivileged))
            return Problem("This automation token created a privileged account; remove its privileged roles before deleting the token.", statusCode: StatusCodes.Status409Conflict);

        _db.AutomationTokens.Remove(token);
        await _db.SaveChangesAsync(ct);

        await auditService.LogAsync(
            CurrentUser.Id,
            action: AuditAction.AutomationTokenDeleted,
            actorId: CurrentUser.Id,
            metadata: new AutomationTokenDeletedMetadata(token.Id, token.Name, createdUsers.Count),
            cancellationToken: ct);

        await transaction.CommitAsync(ct);

        automationTokens.InvalidateCache();

        // Each deleted account's own audit log goes with it, so the log line is the lasting record of what went.
        foreach (var user in createdUsers)
        {
            _logger.LogInformation(
                "Deleted account {UserId} together with automation token {AutomationTokenId}",
                user.Id, token.Id);
        }

        return Ok();
    }
}
