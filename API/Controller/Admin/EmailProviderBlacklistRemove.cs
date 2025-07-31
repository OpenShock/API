using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    [HttpDelete("blacklists/emailProviders/{domain}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveEmailProviderBlacklist([FromRoute] string domain)
    {
        var item = await _db.EmailProviderBlacklists.FindAsync(domain.ToLowerInvariant());
        if (item is null) return NotFound();
        _db.EmailProviderBlacklists.Remove(item);
        await _db.SaveChangesAsync();
        return Ok();
    }
}
