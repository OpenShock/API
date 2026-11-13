namespace OpenShock.Common.Models;

public sealed class WebhookDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}