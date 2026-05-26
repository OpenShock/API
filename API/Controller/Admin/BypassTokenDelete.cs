using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    [HttpDelete("bypassTokens/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBypassToken([FromRoute] Guid id, CancellationToken ct)
    {
        var nDeleted = await _db.BypassTokens.Where(t => t.Id == id).ExecuteDeleteAsync(ct);

        return nDeleted == 0 ? NotFound() : Ok();
    }
}
