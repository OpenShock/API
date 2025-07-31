namespace OpenShock.Common.Models;

public sealed class WebhookDto
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Url { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}