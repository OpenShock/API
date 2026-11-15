using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Models;
using OpenShock.Common.Services.Webhook;

using OpenShock.Common.ApiVersioning;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// List webhooks
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("webhooks")]
    [ApiVersion(1)]
    public async Task<WebhookDto[]> ListWebhooks([FromServices] IWebhookService webhookService)
    {
        PedanticallyEnsureAdmin();

        return await webhookService.GetWebhooksAsync();
    }
}