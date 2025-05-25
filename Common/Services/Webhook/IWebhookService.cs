using System.Drawing;
using OneOf;
using OneOf.Types;
using OpenShock.Common.Models;

namespace OpenShock.Common.Services.Webhook;

public interface IWebhookService
{
    public Task<OneOf<Success<WebhookDto>, UnsupportedWebhookUrl>> AddWebhook(string name, Uri webhookUrl);
    public Task<bool> RemoveWebhook(Guid webhookId);
    public Task<WebhookDto[]> GetWebhooks();

    public Task<OneOf<Success, NotFound, Error, WebhookTimeout>> SendWebhook(string webhookName, string title, string content, Color color);
}

public struct UnsupportedWebhookUrl;
public struct WebhookTimeout;