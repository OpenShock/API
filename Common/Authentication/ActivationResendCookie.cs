using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OpenShock.Common.Constants;
using OpenShock.Common.Options;

namespace OpenShock.Common.Authentication;

/// <summary>
/// Cookie set on signup when the new account still needs activation. It lets the browser that created the account
/// ask for the activation email again ("didn't get the email?") without typing an address, so the resend endpoint
/// can't be aimed at arbitrary inboxes or used to probe which emails are registered.
/// </summary>
/// <remarks>
/// The value is a time-limited data protection payload holding the user id (keys are persisted to the database,
/// so it is valid across API instances). It is removed on any successful session authentication, and by the
/// resend endpoint once there is nothing left to resend.
/// <para>
/// Deliberately <em>not</em> HttpOnly: the frontend reads its presence to decide whether to offer the resend
/// button. HttpOnly would buy little anyway - script on the page could call the endpoint with the cookie attached
/// regardless - and a leaked token can only resend (rate limited) activation emails to that account's own inbox.
/// </para>
/// </remarks>
public static class ActivationResendCookie
{
    private const string ProtectorPurpose = "OpenShock.ActivationResendCookie";

    public static string CreateToken(IDataProtectionProvider provider, Guid userId)
        => GetProtector(provider).Protect(userId.ToString(), Duration.ActivationResendWindow);

    public static void Set(HttpContext context, Guid userId, string domain)
    {
        var provider = context.RequestServices.GetRequiredService<IDataProtectionProvider>();
        var frontendOptions = context.RequestServices.GetRequiredService<FrontendOptions>();

        context.Response.Cookies.Append(AuthConstants.ActivationResendCookieName, CreateToken(provider, userId), new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.Add(Duration.ActivationResendWindow),
            Secure = context.Request.IsHttps || frontendOptions.CookieSecure,
            HttpOnly = false,
            SameSite = SameSiteMode.Lax,
            Domain = domain
        });
    }

    public static bool TryGetUserId(HttpContext context, [NotNullWhen(true)] out Guid? userId)
    {
        userId = null;
        if (!context.Request.Cookies.TryGetValue(AuthConstants.ActivationResendCookieName, out var token) || string.IsNullOrEmpty(token))
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

    /// <summary>Deletes the cookie, if the request carries one, on every configured cookie domain.</summary>
    public static void Remove(HttpContext context)
    {
        if (!context.Request.Cookies.ContainsKey(AuthConstants.ActivationResendCookieName)) return;

        var cookieDomains = context.RequestServices.GetRequiredService<FrontendOptions>().CookieDomains;
        foreach (var domain in cookieDomains)
        {
            context.Response.Cookies.Delete(AuthConstants.ActivationResendCookieName, new CookieOptions { Domain = domain });
        }
    }

    private static ITimeLimitedDataProtector GetProtector(IDataProtectionProvider provider)
        => provider.CreateProtector(ProtectorPurpose).ToTimeLimitedDataProtector();
}
