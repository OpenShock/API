using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using OpenShock.Common.Constants;
using OpenShock.Common.Options;
using OpenShock.Common.Services.Geo;
using OpenShock.Common.Utils;

namespace OpenShock.Common.Authentication;

/// <summary>
/// Marks the browser that created a not yet activated account. The activation endpoint accepts it, together
/// with the emailed link, in place of the account's password or a sign-in with its linked provider.
/// </summary>
/// <remarks>
/// Issued and removed alongside <see cref="ActivationResendCookie"/>, but kept separate because it is a
/// credential and that one is not. The resend cookie has to be readable by the frontend and shared across
/// the cookie domain, which means any sibling subdomain could plant one in a visitor's browser. Planting
/// this one would let someone who signed up with another person's address have that person's click on the
/// emailed link activate the account. So this cookie is bound to the API host alone: no <c>Domain</c>,
/// <c>HttpOnly</c>, and over HTTPS the <c>__Host-</c> name prefix, which browsers refuse to accept from
/// anything but this exact host.
/// <para>
/// It also carries the country the signup came from, when that could be resolved. Presented from a
/// different country it is ignored, which only means the caller is asked for the password or provider
/// sign-in like any other browser. Country and not address: addresses change too often on mobile networks
/// for an exact match to be worth the false alarms. A request whose country cannot be resolved is let
/// through, so a missing GeoIP database does not disable the cookie.
/// </para>
/// </remarks>
public static class SignupBrowserCookie
{
    private const string ProtectorPurpose = "OpenShock.SignupBrowserCookie";
    private const char PayloadSeparator = '|';

    /// <summary>The cookie name used for a request: <c>__Host-</c> prefixed whenever cookies are secure.</summary>
    public static string GetName(HttpContext context) => IsSecure(context)
        ? AuthConstants.SignupBrowserSecureCookieName
        : AuthConstants.SignupBrowserCookieName;

    /// <param name="provider"></param>
    /// <param name="userId"></param>
    /// <param name="countryCode">Country the signup came from, or null when it is not known.</param>
    public static string CreateToken(IDataProtectionProvider provider, Guid userId, string? countryCode = null)
        => GetProtector(provider).Protect($"{userId}{PayloadSeparator}{countryCode}", Duration.ActivationResendWindow);

    public static void Set(HttpContext context, Guid userId)
    {
        var provider = context.RequestServices.GetRequiredService<IDataProtectionProvider>();

        context.Response.Cookies.Append(GetName(context), CreateToken(provider, userId, GetCountryCode(context)), new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.Add(Duration.ActivationResendWindow),
            Secure = IsSecure(context),
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
    }

    public static bool TryGetUserId(HttpContext context, [NotNullWhen(true)] out Guid? userId)
    {
        userId = null;

        // Only the name that matches this deployment: over HTTPS an unprefixed cookie is exactly what a
        // sibling subdomain could have planted.
        if (!context.Request.Cookies.TryGetValue(GetName(context), out var token) || string.IsNullOrEmpty(token))
            return false;

        var provider = context.RequestServices.GetRequiredService<IDataProtectionProvider>();
        try
        {
            var payload = GetProtector(provider).Unprotect(token).AsSpan();
            var separator = payload.IndexOf(PayloadSeparator);
            if (separator < 0 || !Guid.TryParse(payload[..separator], out var id)) return false;

            // Only a known mismatch counts: unknown on either side says nothing.
            var signupCountry = payload[(separator + 1)..];
            if (!signupCountry.IsEmpty
                && GetCountryCode(context) is { } currentCountry
                && !signupCountry.Equals(currentCountry, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            userId = id;
            return true;
        }
        catch (CryptographicException)
        {
            // Tampered, expired, or signed with a revoked key.
            return false;
        }
    }

    /// <summary>Deletes the cookie, if the request carries one, under either of its names.</summary>
    /// <remarks>
    /// Goes by what the request carries instead of <see cref="GetName"/>: this runs on every request a
    /// session authenticates, also in services that never issue the cookie and have no
    /// <see cref="FrontendOptions"/> registered (the LiveControlGateway).
    /// </remarks>
    public static void Remove(HttpContext context)
    {
        // A __Host- cookie only exists as a secure one, and browsers ignore its removal unless that is secure too.
        if (context.Request.Cookies.ContainsKey(AuthConstants.SignupBrowserSecureCookieName))
            Delete(context, AuthConstants.SignupBrowserSecureCookieName, secure: true);

        if (context.Request.Cookies.ContainsKey(AuthConstants.SignupBrowserCookieName))
            Delete(context, AuthConstants.SignupBrowserCookieName, secure: context.Request.IsHttps);
    }

    private static void Delete(HttpContext context, string name, bool secure)
    {
        context.Response.Cookies.Delete(name, new CookieOptions
        {
            Secure = secure,
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
    }

    private static string? GetCountryCode(HttpContext context)
    {
        var countryCode = context.RequestServices.GetRequiredService<IIpEnrichmentService>().Enrich(context.GetRemoteIP())?.CountryCode;
        return string.IsNullOrEmpty(countryCode) ? null : countryCode;
    }

    private static bool IsSecure(HttpContext context)
        => context.Request.IsHttps || context.RequestServices.GetRequiredService<FrontendOptions>().CookieSecure;

    private static ITimeLimitedDataProtector GetProtector(IDataProtectionProvider provider)
        => provider.CreateProtector(ProtectorPurpose).ToTimeLimitedDataProtector();
}
