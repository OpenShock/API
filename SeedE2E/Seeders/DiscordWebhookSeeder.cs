using Bogus;
using Microsoft.Extensions.Logging;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;

namespace OpenShock.SeedE2E.Seeders;

public static class DiscordWebhookSeeder
{
    public static async Task SeedAsync(OpenShockContext db, ILogger logger)
    {
        if (db.DiscordWebhooks.Any())
            return;

        logger.LogInformation("Generating DiscordWebhooks...");

        var webhookFaker = new Faker<DiscordWebhook>()
            .RuleFor(w => w.Name, f => f.Internet.UserName().Truncate(50))
            .RuleFor(w => w.WebhookId, f => f.Random.Long())
            .RuleFor(w => w.WebhookToken, f => f.Random.AlphaNumeric(30))
            .RuleFor(w => w.CreatedAt, f => f.Date.RecentOffset(30).UtcDateTime);

        // Generate a handful of webhooks
        var webhooks = webhookFaker.Generate(5);
        db.DiscordWebhooks.AddRange(webhooks);
        await db.SaveChangesAsync();
    }
}
