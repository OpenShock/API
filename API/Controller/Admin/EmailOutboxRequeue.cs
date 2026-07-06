using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.RedisPubSub;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Requeues a terminal (sent/failed/skipped) email outbox message: resets it to pending, due now,
    /// with a fresh attempt budget, and nudges the delivery consumer to send it immediately.
    /// </summary>
    /// <response code="200">Requeued</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">No such message</response>
    /// <response code="409">Message is pending or currently being sent</response>
    [HttpPost("email/outbox/{id}/requeue")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RequeueEmailOutbox([FromRoute] Guid id, [FromServices] IRedisPubService redisPubService, CancellationToken ct)
    {
        PedanticallyEnsureAdmin();

        var nowUtc = DateTime.UtcNow;

        // Guard on a terminal status so a concurrent Cron send (Sending) or an already-queued row
        // (Pending) is left alone - the WHERE simply matches nothing rather than racing the send.
        var updated = await _db.EmailOutbox
            .Where(m => m.Id == id && (m.Status == EmailStatus.Failed || m.Status == EmailStatus.Skipped || m.Status == EmailStatus.Sent))
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Status, EmailStatus.Pending)
                .SetProperty(m => m.NextAttemptAt, nowUtc)
                .SetProperty(m => m.AttemptCount, 0)
                .SetProperty(m => m.LastError, (string?)null)
                .SetProperty(m => m.FailedAt, (DateTime?)null)
                .SetProperty(m => m.SentAt, (DateTime?)null), ct);

        if (updated == 0)
        {
            var exists = await _db.EmailOutbox.AnyAsync(m => m.Id == id, ct);
            return exists ? Conflict() : NotFound();
        }

        await redisPubService.SendEmailOutboxPending();

        return Ok();
    }
}
