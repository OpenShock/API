using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Controller.Admin.DTOs;
using OpenShock.Common.Services.Bypass;
using OpenShock.Common.Utils;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    [HttpPost("bypassTokens/{id}/rotate")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<CreatedBypassTokenDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RotateBypassToken([FromRoute] Guid id, CancellationToken ct)
    {
        var token = await _db.BypassTokens.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (token is null) return NotFound();

        var secret = IBypassTokenService.GenerateSecret();
        token.TokenHash = HashingUtils.HashToken(secret);
        token.LastUsedAt = null;
        token.LastUsedByUserId = null;
        token.UseCount = 0;
        token.LastRotatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        // BypassTokenUserUse rows are intentionally NOT cleared — the auto-cleanup pact is
        // per-user-account, not per-secret-version. Rotating doesn't pardon previously-used accounts.

        return Ok(new CreatedBypassTokenDto
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
