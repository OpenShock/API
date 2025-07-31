using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Controller.Admin.DTOs;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    [HttpPost("blacklists/emailProviders")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AddEmailProviderBlacklist([FromBody] AddEmailProviderBlacklistDto dto)
    {
        var entry = new EmailProviderBlacklist
        {
            Domain = dto.Domain.ToLowerInvariant()
        };
        _db.EmailProviderBlacklists.Add(entry);
        await _db.SaveChangesAsync();
        return Ok();
    }
}
