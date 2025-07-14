namespace OpenShock.Common.OpenShockDb;

public sealed class DiscordWebhook
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }

    public required long WebhookId { get; set; } // TODO: This should probably be ulong
    
    public required string WebhookToken { get; set; }

    public DateTime CreatedAt { get; set; }
}