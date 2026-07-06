using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Controller.Admin.DTOs;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.RedisPubSub;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Requeues many email outbox messages at once. Each id is treated exactly like the single-message
    /// requeue: only terminal (sent/failed/skipped) rows are affected; pending/sending rows and missing
    /// ids are skipped. Returns how many rows were actually requeued.
    /// </summary>
    /// <response code="200">Bulk requeue applied</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("email/outbox/bulk/requeue")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<EmailOutboxBulkResultDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public async Task<EmailOutboxBulkResultDto> BulkRequeueEmailOutbox([FromBody] EmailOutboxBulkRequest body, [FromServices] IRedisPubService redisPubService, CancellationToken ct)
    {
        PedanticallyEnsureAdmin();

        var nowUtc = DateTime.UtcNow;

        var affected = await _db.EmailOutbox
            .Where(m => body.Ids.Contains(m.Id) && (m.Status == EmailStatus.Failed || m.Status == EmailStatus.Skipped || m.Status == EmailStatus.Sent))
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Status, EmailStatus.Pending)
                .SetProperty(m => m.NextAttemptAt, nowUtc)
                .SetProperty(m => m.AttemptCount, 0)
                .SetProperty(m => m.LastError, (string?)null)
                .SetProperty(m => m.FailedAt, (DateTime?)null)
                .SetProperty(m => m.SentAt, (DateTime?)null), ct);

        if (affected > 0)
        {
            await redisPubService.SendEmailOutboxPending();
        }

        return new EmailOutboxBulkResultDto { Requested = body.Ids.Length, Affected = affected };
    }

    /// <summary>
    /// Cancels many still-pending email outbox messages at once by marking them skipped. Rows that are
    /// not pending (already sending or terminal) and missing ids are skipped. Returns how many rows were
    /// actually cancelled.
    /// </summary>
    /// <response code="200">Bulk cancel applied</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("email/outbox/bulk/cancel")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<EmailOutboxBulkResultDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public async Task<EmailOutboxBulkResultDto> BulkCancelEmailOutbox([FromBody] EmailOutboxBulkRequest body, CancellationToken ct)
    {
        PedanticallyEnsureAdmin();

        var affected = await _db.EmailOutbox
            .Where(m => body.Ids.Contains(m.Id) && m.Status == EmailStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Status, EmailStatus.Skipped)
                .SetProperty(m => m.LastError, "Cancelled by admin"), ct);

        return new EmailOutboxBulkResultDto { Requested = body.Ids.Length, Affected = affected };
    }

    /// <summary>
    /// Permanently deletes many email outbox messages at once. Missing ids are skipped. Returns how many
    /// rows were actually deleted.
    /// </summary>
    /// <response code="200">Bulk delete applied</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("email/outbox/bulk/delete")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<EmailOutboxBulkResultDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public async Task<EmailOutboxBulkResultDto> BulkDeleteEmailOutbox([FromBody] EmailOutboxBulkRequest body, CancellationToken ct)
    {
        PedanticallyEnsureAdmin();

        var affected = await _db.EmailOutbox
            .Where(m => body.Ids.Contains(m.Id))
            .ExecuteDeleteAsync(ct);

        return new EmailOutboxBulkResultDto { Requested = body.Ids.Length, Affected = affected };
    }
}
