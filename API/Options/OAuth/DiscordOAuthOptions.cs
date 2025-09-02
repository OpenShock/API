namespace OpenShock.API.Options.OAuth;

public sealed class DiscordOAuthOptions
{
    public const string SectionName = "OpenShock:OAuth2:Discord";

    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public required PathString CallbackPath { get; init; }
    public required PathString AccessDeniedPath { get; init; }
}