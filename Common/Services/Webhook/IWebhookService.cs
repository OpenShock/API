using System.Drawing;
using OneOf;
using OneOf.Types;
using OpenShock.Common.Models;

namespace OpenShock.Common.Services.Webhook;

public interface IWebhookService
{
    public Task<OneOf<Success<WebhookDto>, UnsupportedWebhookUrl>> AddWebhookAsync(string name, Uri webhookUrl);
    public Task<bool> RemoveWebhookAsync(Guid webhookId);
    public Task<WebhookDto[]> GetWebhooksAsync();

    public Task<OneOf<Success, NotFound, Error, WebhookTimeout>> SendWebhookAsync(string webhookName, string title, string content, Color color);
}

public struct UnsupportedWebhookUrl;
public struct WebhookTimeout;