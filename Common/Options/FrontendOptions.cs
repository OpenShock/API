namespace OpenShock.Common.Options;

public sealed class FrontendOptions
{
    public required Uri BaseUrl { get; init; }
    public required Uri ShortUrl { get; init; }
    public required IReadOnlyCollection<string> CookieDomains { get; init; }
}