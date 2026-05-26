using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Controller.Admin.DTOs;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    [HttpGet("bypassTokens")]
    public async IAsyncEnumerable<BypassTokenDto> ListBypassTokens()
    {
        await foreach (var token in _db.BypassTokens.AsNoTracking().AsAsyncEnumerable())
        {
            yield return new BypassTokenDto
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
            };
        }
    }
}
