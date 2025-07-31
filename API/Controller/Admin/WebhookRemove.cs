using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Errors;
using OpenShock.Common.Services.Webhook;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Removes a webhook
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpDelete("webhooks/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveWebhook([FromRoute] Guid id, [FromServices] IWebhookService webhookService)
    {
        bool removed = await webhookService.RemoveWebhookAsync(id);
        return removed ? Ok() : Problem(AdminError.WebhookNotFound);
    }
}