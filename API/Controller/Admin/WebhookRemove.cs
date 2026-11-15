using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Errors;
using OpenShock.Common.Services.Webhook;

using OpenShock.Common.ApiVersioning;

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
    [ApiVersion(1)]
    public async Task<IActionResult> RemoveWebhook([FromRoute] Guid id, [FromServices] IWebhookService webhookService)
    {
        PedanticallyEnsureAdmin();

        bool removed = await webhookService.RemoveWebhookAsync(id);
        return removed ? Ok() : Problem(AdminError.WebhookNotFound);
    }
}