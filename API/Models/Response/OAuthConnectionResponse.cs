namespace OpenShock.API.Models.Response;

public sealed class OAuthConnectionResponse
{
    public required string ProviderKey { get; init; }
    public required string ExternalId { get; init; }
    public required string? DisplayName { get; init; }
    public required DateTime LinkedAt { get; init; }
}