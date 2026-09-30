using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Controller.Admin.DTOs;
using OpenShock.Common.Errors;
using OpenShock.Common.Models;
using OpenShock.Common.Results;
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
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddWebhook([FromBody] AddWebhookDto body, [FromServices] IWebhookService webhookService)
    {
        PedanticallyEnsureAdmin();

        var result = await webhookService.AddWebhookAsync(body.Name, body.Url);
        return result switch
        {
            Success<WebhookDto> success => Ok(success.Value),
            UnsupportedWebhookUrl => Problem(AdminError.WebhookOnlyDiscord)
        };
    }
}