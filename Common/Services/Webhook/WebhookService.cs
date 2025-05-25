using System.Drawing;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.Common.Services.Webhook;

public sealed class WebhookService : IWebhookService
{
    private readonly OpenShockContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    
    public WebhookService(OpenShockContext db, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
    }

    private static string GetWebhookUrl(long webhookId, string webhookToken) =>
        $"https://discord.com/api/webhooks/{webhookId}/{webhookToken}";
    
    public async Task<OneOf<Success<WebhookDto>, UnsupportedWebhookUrl>> AddWebhook(string name, Uri webhookUrl)
    {
        if (webhookUrl is not
            {
                Scheme: "https",
                DnsSafeHost: "discord.com",
                Segments: ["/", "api/", "webhooks/", {} webhookIdStr, {} webhookToken]
            } ||
            !long.TryParse(webhookIdStr[..^1], out var webhookId)
           )
        {
            return new UnsupportedWebhookUrl();
        }
        
        var webhook = new DiscordWebhook
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            WebhookId = webhookId,
            WebhookToken = webhookToken,
        };
        
        _db.DiscordWebhooks.Add(webhook);

        await _db.SaveChangesAsync();

        return new Success<WebhookDto>(new WebhookDto
        {
            Id = webhook.Id,
            Name = webhook.Name,
            Url = GetWebhookUrl(webhook.WebhookId, webhook.WebhookToken),
            CreatedAt = webhook.CreatedAt
        });
    }

    public async Task<bool> RemoveWebhook(Guid webhookId)
    {
        var nDeleted = await _db.DiscordWebhooks.Where(w => w.Id == webhookId).ExecuteDeleteAsync();
        return nDeleted > 0;
    }

    public async Task<WebhookDto[]> GetWebhooks()
    {
        return await _db.DiscordWebhooks
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WebhookDto
            {
                Id = w.Id,
                Name = w.Name,
                Url = GetWebhookUrl(w.WebhookId, w.WebhookToken),
                CreatedAt = w.CreatedAt
            })
            .ToArrayAsync();
    }

    public async Task<OneOf<Success, NotFound, Error, WebhookTimeout>> SendWebhook(string webhookName, string title, string content, Color color)
    {
        var webhook = await _db.DiscordWebhooks
            .Where(w => w.Name == webhookName)
            .FirstOrDefaultAsync();

        if (webhook is null)
            return new NotFound();

        using var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(10);

        var embed = new
        {
            title,
            description = content,
            color = (color.R << 16) | (color.G << 8) | color.B // RGB to Discord int
        };

        var payload = new
        {
            embeds = new[] { embed }
        };

        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                GetWebhookUrl(webhook.WebhookId, webhook.WebhookToken),
                payload);

            if (!response.IsSuccessStatusCode)
                return new Error(); // Consider inspecting status code if you want more granularity

            return new Success();
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            return new WebhookTimeout();
        }
        catch (Exception)
        {
            return new Error();
        }
    }
}