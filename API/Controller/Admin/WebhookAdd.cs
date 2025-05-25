using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Controller.Admin.DTOs;
using OpenShock.Common.Errors;
using OpenShock.Common.Services.Webhook;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Creates a webhook
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("webhooks")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddWebhook([FromBody] AddWebhookDto body, [FromServices] IWebhookService webhookService)
    {
        var result = await webhookService.AddWebhook(body.Name, body.Url);
        return result.Match<IActionResult>(
            success => Ok(success.Value),
            unsupported => Problem(AdminError.WebhookOnlyDiscord)
        );
    }
}