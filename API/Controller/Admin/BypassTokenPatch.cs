using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Controller.Admin.DTOs;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    [HttpPatch("bypassTokens/{id}")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<BypassTokenDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchBypassToken([FromRoute] Guid id, [FromBody] PatchBypassTokenDto body, CancellationToken ct)
    {
        var token = await _db.BypassTokens.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (token is null) return NotFound();

        if (body.Name is not null) token.Name = body.Name.Trim();
        if (body.Types is not null) token.Types = [.. body.Types.Distinct()];
        if (body.AutoCleanupUsers is not null) token.AutoCleanupUsers = body.AutoCleanupUsers.Value;
        if (body.AutoCleanupAfter is not null) token.AutoCleanupAfter = body.AutoCleanupAfter;

        if (token.AutoCleanupUsers && token.AutoCleanupAfter is null)
            return Problem("AutoCleanupAfter is required when AutoCleanupUsers is true.", statusCode: StatusCodes.Status400BadRequest);

        await _db.SaveChangesAsync(ct);

        return Ok(new BypassTokenDto
        {
            Id = token.Id,
            Name = token.Name,
            Types = token.Types,
            CreatedAt = token.CreatedAt,
            LastUsedAt = token.LastUsedAt,
            LastUsedByUserId = token.LastUsedByUserId,
            LastRotatedAt = token.LastRotatedAt,
            UseCount = token.UseCount,
            AutoCleanupUsers = token.AutoCleanupUsers,
            AutoCleanupAfter = token.AutoCleanupAfter,
        });
    }
}
