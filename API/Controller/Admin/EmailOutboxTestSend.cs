using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Controller.Admin.DTOs;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.RedisPubSub;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Enqueues a preview/test email of the chosen type to the chosen address. It flows through the
    /// normal durable delivery pipeline - visible in the outbox listing - but is rendered with
    /// placeholder data and a dummy link, so it touches no request row and mints no token.
    /// </summary>
    /// <response code="200">Test email enqueued</response>
    /// <response code="400">Invalid recipient address</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("email/test")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<EmailOutboxMessageDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendTestEmail([FromBody] SendTestEmailDto body, [FromServices] IRedisPubService redisPubService, CancellationToken ct)
    {
        PedanticallyEnsureAdmin();

        // CreatedAt / NextAttemptAt / AttemptCount are filled by their DB defaults (see the context
        // mapping) and read back after insert, so the returned DTO carries the real values.
        var message = EmailOutboxMessage.ForPreview(body.Type, body.Recipient, body.RecipientName);

        _db.EmailOutbox.Add(message);
        await _db.SaveChangesAsync(ct);

        await redisPubService.SendEmailOutboxPending();

        return Ok(EmailOutboxMessageDto.FromModel(message));
    }
}
