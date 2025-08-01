using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Controller.Admin.DTOs;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    [HttpPost("blacklist/emailProviders")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AddEmailProviderBlacklist([FromBody] AddEmailProviderBlacklistDto dto, CancellationToken ct)
    {
        var existingDomains = await _db.EmailProviderBlacklists.Select(x => x.Domain.ToLowerInvariant()).ToHashSetAsync(ct);

        foreach (var domain in dto.Domains)
        {
            if (string.IsNullOrWhiteSpace(domain)) continue;
            
            var normalized = domain.Trim().ToLowerInvariant();
            
            if (!existingDomains.Add(normalized)) continue;

            _db.EmailProviderBlacklists.Add(new EmailProviderBlacklist
            {
                Id = Guid.CreateVersion7(),
                Domain = normalized,
            });
        }
        
        await _db.SaveChangesAsync(ct);
        
        return Ok();
    }
}
