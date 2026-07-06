using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Cancels a still-pending email outbox message by marking it skipped, so it is never sent. Only
    /// pending messages can be cancelled; a message already being sent or in a terminal state cannot.
    /// </summary>
    /// <response code="200">Cancelled</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">No such message</response>
    /// <response code="409">Message is not pending</response>
    [HttpPost("email/outbox/{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelEmailOutbox([FromRoute] Guid id, CancellationToken ct)
    {
        PedanticallyEnsureAdmin();

        var updated = await _db.EmailOutbox
            .Where(m => m.Id == id && m.Status == EmailStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Status, EmailStatus.Skipped)
                .SetProperty(m => m.LastError, "Cancelled by admin"), ct);

        if (updated == 0)
        {
            var exists = await _db.EmailOutbox.AnyAsync(m => m.Id == id, ct);
            return exists ? Conflict() : NotFound();
        }

        return Ok();
    }
}
