

namespace OpenShock.API.Options.OAuth;

public sealed class GoogleOAuthOptions
{
    public const string SectionName = "OpenShock:OAuth2:Google";

    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
}