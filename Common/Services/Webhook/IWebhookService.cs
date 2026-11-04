using System.Drawing;
using OpenShock.Common.Models;
using OpenShock.Common.Results;

namespace OpenShock.Common.Services.Webhook;

public interface IWebhookService
{
    public Task<Union2<Success<WebhookDto>, UnsupportedWebhookUrl>> AddWebhookAsync(string name, Uri webhookUrl, CancellationToken cancellationToken = default);
    public Task<bool> RemoveWebhookAsync(Guid webhookId, CancellationToken cancellationToken = default);
    public Task<WebhookDto[]> GetWebhooksAsync(CancellationToken cancellationToken = default);

    public Task<Union4<Success, NotFound, Error, WebhookTimeout>> SendWebhookAsync(string webhookName, string title, string content, Color color, CancellationToken cancellationToken = default);
}

public struct UnsupportedWebhookUrl;
public struct WebhookTimeout;