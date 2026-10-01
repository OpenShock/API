using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.Audit;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Deletes an automation token together with every account it created. Refused while any of those
    /// accounts holds a privileged role
    /// </summary>
    [HttpDelete("automationTokens/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAutomationToken(
        [FromRoute] Guid id,
        [FromServices] IAuditService auditService,
        CancellationToken ct)
    {
        PedanticallyEnsureAdmin();

        var token = await _db.AutomationTokens.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
        if (token is null) return NotFound();

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var createdUsers = await _db.Users
            .Where(u => u.CreatedByAutomationTokenId == id)
            .Select(u => new { u.Id, u.Roles })
            .ToListAsync(ct);

        if (createdUsers.Any(u => PrivilegedRoles.Any(u.Roles))) return PrivilegedAccountConflict();

        var userIds = createdUsers.Select(u => u.Id).ToArray();

        // The roles are checked again as the rows are deleted, so an account promoted since it was read is kept.
        await _db.Database.ExecuteSqlAsync(
            $"DELETE FROM users WHERE id = ANY({userIds}) AND NOT (roles && {PrivilegedRoles.All})", ct);

        // The foreign key restricts, so this fails while any account still references the token: a privileged
        // one, or one created or promoted meanwhile. On success, exactly userIds went with it.
        try
        {
            await _db.AutomationTokens.Where(t => t.Id == id).ExecuteDeleteAsync(ct);
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            return PrivilegedAccountConflict();
        }

        await auditService.LogAsync(
            CurrentUser.Id,
            action: AuditAction.AutomationTokenDeleted,
            actorId: CurrentUser.Id,
            metadata: new AutomationTokenDeletedMetadata(token.Id, token.Name, userIds.Length),
            cancellationToken: ct);

        await transaction.CommitAsync(ct);

        // Each deleted account's own audit log goes with it, so the log line is the lasting record of what went.
        foreach (var userId in userIds)
        {
            _logger.LogInformation(
                "Deleted account {UserId} together with automation token {AutomationTokenId}",
                userId, token.Id);
        }

        return Ok();

        IActionResult PrivilegedAccountConflict() => Problem(
            "This automation token created a privileged account; remove its privileged roles before deleting the token.",
            statusCode: StatusCodes.Status409Conflict);
    }
}
