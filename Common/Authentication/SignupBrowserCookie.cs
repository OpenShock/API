using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using OpenShock.Common.Constants;
using OpenShock.Common.Options;

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
/// </remarks>
public static class SignupBrowserCookie
{
    private const string ProtectorPurpose = "OpenShock.SignupBrowserCookie";

    /// <summary>The cookie name used for a request: <c>__Host-</c> prefixed whenever cookies are secure.</summary>
    public static string GetName(HttpContext context) => IsSecure(context)
        ? AuthConstants.SignupBrowserSecureCookieName
        : AuthConstants.SignupBrowserCookieName;

    public static string CreateToken(IDataProtectionProvider provider, Guid userId)
        => GetProtector(provider).Protect(userId.ToString(), Duration.ActivationResendWindow);

    public static void Set(HttpContext context, Guid userId)
    {
        var provider = context.RequestServices.GetRequiredService<IDataProtectionProvider>();

        context.Response.Cookies.Append(GetName(context), CreateToken(provider, userId), new CookieOptions
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
            if (!Guid.TryParse(GetProtector(provider).Unprotect(token), out var id)) return false;
            userId = id;
            return true;
        }
        catch (CryptographicException)
        {
            // Tampered, expired, or signed with a revoked key.
            return false;
        }
    }

    /// <summary>Deletes the cookie, if the request carries one.</summary>
    public static void Remove(HttpContext context)
    {
        var name = GetName(context);
        if (!context.Request.Cookies.ContainsKey(name)) return;

        context.Response.Cookies.Delete(name, new CookieOptions
        {
            Secure = IsSecure(context),
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
    }

    private static bool IsSecure(HttpContext context)
        => context.Request.IsHttps || context.RequestServices.GetRequiredService<FrontendOptions>().CookieSecure;

    private static ITimeLimitedDataProtector GetProtector(IDataProtectionProvider provider)
        => provider.CreateProtector(ProtectorPurpose).ToTimeLimitedDataProtector();
}
