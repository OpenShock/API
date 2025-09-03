namespace OpenShock.API.OAuth;

// what we return to frontend at /oauth/discord/data
public sealed class OAuthPublic
{
    public required string Provider { get; init; }
    public required string? Email { get; init; }
    public required string? DisplayName { get; init; }
    public required DateTime ExpiresAt { get; init; }
}