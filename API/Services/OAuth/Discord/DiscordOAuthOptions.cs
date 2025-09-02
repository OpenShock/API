namespace OpenShock.API.Services.OAuth.Discord;

public sealed class DiscordOAuthOptions
{
    public const string SectionName = "OpenShock:OAuth2:Discord";

    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
}