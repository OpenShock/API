namespace OpenShock.API.Models.Response;

public sealed class OAuthConnectionResponse
{
    public required string ProviderName { get; init; }
    public required string? ProviderAccountName { get; init; }
}