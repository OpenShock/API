using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Models;
using OpenShock.Common.Services.Webhook;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// List webhooks
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("webhooks")]
    public async Task<WebhookDto[]> ListWebhooks([FromServices] IWebhookService webhookService)
    {
        return await webhookService.GetWebhooks();
    }
}