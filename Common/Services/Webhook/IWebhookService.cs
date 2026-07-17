using System.Drawing;
using OpenShock.Common.Models;
using OpenShock.Common.Results;

namespace OpenShock.Common.Services.Webhook;

public interface IWebhookService
{
    public Task<Union2<Success<WebhookDto>, UnsupportedWebhookUrl>> AddWebhookAsync(string name, Uri webhookUrl);
    public Task<bool> RemoveWebhookAsync(Guid webhookId);
    public Task<WebhookDto[]> GetWebhooksAsync();

    public Task<Union4<Success, NotFound, Error, WebhookTimeout>> SendWebhookAsync(string webhookName, string title, string content, Color color);
}

public struct UnsupportedWebhookUrl;
public struct WebhookTimeout;