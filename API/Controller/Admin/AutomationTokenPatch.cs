using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Controller.Admin.DTOs;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.Audit;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Updates an automation token
    /// </summary>
    [HttpPatch("automationTokens/{id}")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<AutomationTokenDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchAutomationToken(
        [FromRoute] Guid id,
        [FromBody] PatchAutomationTokenDto body,
        [FromServices] IAuditService auditService,
        CancellationToken ct)
    {
        PedanticallyEnsureAdmin();

        var token = await _db.AutomationTokens.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (token is null) return NotFound();

        var autoCleanupUsers = body.AutoCleanupUsers ?? token.AutoCleanupUsers;

        // Switching cleanup off without naming a delay clears the delay, so it can be unset at all.
        var autoCleanupAfter = body.AutoCleanupAfter ??
                               (body.AutoCleanupUsers == false ? null : token.AutoCleanupAfter);

        if (autoCleanupUsers && autoCleanupAfter is null)
            return Problem("AutoCleanupAfter is required when AutoCleanupUsers is true.", statusCode: StatusCodes.Status400BadRequest);

        if (body.Name is not null) token.Name = body.Name.Trim();
        if (body.Types is not null) token.Types = [.. body.Types.Distinct()];
        token.AutoCleanupUsers = autoCleanupUsers;
        token.AutoCleanupAfter = autoCleanupAfter;

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        await _db.SaveChangesAsync(ct);

        await auditService.LogAsync(
            CurrentUser.Id,
            action: AuditAction.AutomationTokenUpdated,
            actorId: CurrentUser.Id,
            metadata: new AutomationTokenUpdatedMetadata(token.Id, token.Name, [.. token.Types.Select(t => t.ToString())], token.AutoCleanupUsers, token.AutoCleanupAfter),
            cancellationToken: ct);

        await transaction.CommitAsync(ct);

        return Ok(AutomationTokenDto.FromEntity(token));
    }
}
