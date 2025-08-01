using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    [HttpDelete("blacklist/emailProviders/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveEmailProviderBlacklist([FromRoute] Guid id, CancellationToken ct)
    {
        var nDeleted = await _db.EmailProviderBlacklists.Where(x => x.Id == id).ExecuteDeleteAsync(ct);

        return nDeleted == 0 ? NotFound() : Ok();
    }
}
