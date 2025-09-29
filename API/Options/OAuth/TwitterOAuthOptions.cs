

namespace OpenShock.API.Options.OAuth;

public sealed class TwitterOAuthOptions
{
    public const string SectionName = "OpenShock:OAuth2:Twitter";

    public required string ConsumerKey { get; init; }
    public required string ConsumerSecret { get; init; }
}