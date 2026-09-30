using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Controller.Admin.DTOs;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.Audit;
using OpenShock.Common.Services.AutomationTokens;
using OpenShock.Common.Utils;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Rotates the secret of an automation token. The new secret is only returned once, in this response.
    /// Other API instances may keep accepting the old secret for up to 30 seconds
    /// </summary>
    [HttpPost("automationTokens/{id}/rotate")]
    [ProducesResponseType<CreatedAutomationTokenDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RotateAutomationToken(
        [FromRoute] Guid id,
        [FromServices] IAutomationTokenService automationTokens,
        [FromServices] IAuditService auditService,
        CancellationToken ct)
    {
        PedanticallyEnsureAdmin();

        var token = await _db.AutomationTokens.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (token is null) return NotFound();

        var secret = IAutomationTokenService.GenerateSecret();
        token.TokenHash = HashingUtils.HashToken(secret);
        token.LastUsedAt = null;
        token.UseCount = 0;
        token.LastRotatedAt = DateTime.UtcNow;

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        await _db.SaveChangesAsync(ct);

        await auditService.LogAsync(
            CurrentUser.Id,
            action: AuditAction.AutomationTokenRotated,
            actorId: CurrentUser.Id,
            metadata: new AutomationTokenRotatedMetadata(token.Id, token.Name),
            cancellationToken: ct);

        await transaction.CommitAsync(ct);

        automationTokens.InvalidateCache();

        // Accounts the token created stay linked to it (and to its cleanup schedule) across rotations,
        // since the token id doesn't change.

        return Ok(new CreatedAutomationTokenDto
        {
            Id = token.Id,
            Name = token.Name,
            Secret = secret,
            Types = token.Types,
            CreatedAt = token.CreatedAt,
            LastRotatedAt = token.LastRotatedAt,
            AutoCleanupUsers = token.AutoCleanupUsers,
            AutoCleanupAfter = token.AutoCleanupAfter,
        });
    }
}
