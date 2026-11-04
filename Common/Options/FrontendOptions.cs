namespace OpenShock.Common.Options;

public sealed class FrontendOptions
{
    public required Uri BaseUrl { get; init; }
    public required Uri ShortUrl { get; init; }
    public required IReadOnlyCollection<string> CookieDomains { get; init; }

    /// <summary>
    /// Whether the configured <see cref="BaseUrl"/> scheme calls for auth cookies to be flagged <c>Secure</c>.
    /// This is one of two signals - the request scheme is the other, and either being HTTPS is enough - so an
    /// <c>http://</c> base URL only yields non-secure cookies on a plain-HTTP request, as in dev and integration
    /// tests, where the browser must be able to store and resend them.
    /// </summary>
    public bool CookieSecure => BaseUrl.Scheme == Uri.UriSchemeHttps;
}