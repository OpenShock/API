using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    [HttpDelete("blacklists/usernames/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveUsernameBlacklist([FromRoute] Guid id)
    {
        var entity = await _db.UserNameBlacklists.FindAsync(id);
        if (entity is null) return NotFound();
        _db.UserNameBlacklists.Remove(entity);
        await _db.SaveChangesAsync();
        return Ok();
    }
}
