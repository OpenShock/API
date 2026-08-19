namespace OpenShock.Common.Options;

public sealed class FrontendOptions
{
    public required Uri BaseUrl { get; init; }
    public required Uri ShortUrl { get; init; }
    public required IReadOnlyCollection<string> CookieDomains { get; init; }

    /// <summary>
    /// Whether auth cookies should be flagged <c>Secure</c>, derived from the configured <see cref="BaseUrl"/> scheme.
    /// An <c>http://</c> base URL (dev / integration tests over plain HTTP) yields non-secure cookies so the browser
    /// can store and resend them; an <c>https://</c> base URL keeps cookies <c>Secure</c>-only as in production.
    /// </summary>
    public bool CookieSecure => BaseUrl.Scheme == Uri.UriSchemeHttps;
}