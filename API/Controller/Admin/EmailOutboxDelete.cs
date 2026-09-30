using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Permanently deletes an email outbox message. If the delivery consumer is mid-send it simply finds
    /// the row gone and does nothing, so a delete never corrupts an in-flight send.
    /// </summary>
    /// <response code="200">Deleted</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">No such message</response>
    [HttpDelete("email/outbox/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEmailOutbox([FromRoute] Guid id, CancellationToken ct)
    {
        PedanticallyEnsureAdmin();

        var nDeleted = await _db.EmailOutbox.Where(m => m.Id == id).ExecuteDeleteAsync(ct);

        return nDeleted == 0 ? NotFound() : Ok();
    }
}
