using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Controller.Admin.DTOs;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.Audit;
using OpenShock.Common.Services.AutomationTokens;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Updates an automation token. Other API instances may keep the old settings for up to 30 seconds
    /// </summary>
    [HttpPatch("automationTokens/{id}")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<AutomationTokenDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchAutomationToken(
        [FromRoute] Guid id,
        [FromBody] PatchAutomationTokenDto body,
        [FromServices] IAutomationTokenService automationTokens,
        [FromServices] IAuditService auditService,
        CancellationToken ct)
    {
        PedanticallyEnsureAdmin();

        var token = await _db.AutomationTokens.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (token is null) return NotFound();

        if (body.Name is not null) token.Name = body.Name.Trim();
        if (body.Types is not null) token.Types = [.. body.Types.Distinct()];
        if (body.AutoCleanupAfter is not null) token.AutoCleanupAfter = body.AutoCleanupAfter;
        if (body.AutoCleanupUsers is not null)
        {
            token.AutoCleanupUsers = body.AutoCleanupUsers.Value;

            // Switching cleanup off without naming a delay clears the delay, so it can be unset at all.
            if (!token.AutoCleanupUsers && body.AutoCleanupAfter is null) token.AutoCleanupAfter = null;
        }

        if (token.AutoCleanupUsers && token.AutoCleanupAfter is null)
            return Problem("AutoCleanupAfter is required when AutoCleanupUsers is true.", statusCode: StatusCodes.Status400BadRequest);

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        await _db.SaveChangesAsync(ct);

        await auditService.LogAsync(
            CurrentUser.Id,
            action: AuditAction.AutomationTokenUpdated,
            actorId: CurrentUser.Id,
            metadata: new AutomationTokenUpdatedMetadata(token.Id, token.Name, [.. token.Types.Select(t => t.ToString())], token.AutoCleanupUsers, token.AutoCleanupAfter),
            cancellationToken: ct);

        await transaction.CommitAsync(ct);

        automationTokens.InvalidateCache();

        return Ok(AutomationTokenDto.FromEntity(token));
    }
}
