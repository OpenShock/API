using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Controller.Admin.DTOs;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    [HttpPost("blacklist/emailProviders")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AddEmailProviderBlacklist([FromBody] AddEmailProviderBlacklistDto dto)
    {
        var entry = new EmailProviderBlacklist
        {
            Id = Guid.CreateVersion7(),
            Domain = dto.Domain.ToLowerInvariant()
        };
        _db.EmailProviderBlacklists.Add(entry);
        await _db.SaveChangesAsync();
        return Ok();
    }
}
