using OpenShock.Common.Constants;
using System.Diagnostics.CodeAnalysis;

namespace OpenShock.Common.Utils;

public static class AuthUtils
{
    private static readonly string[] TokenHeaderNames = [
        AuthConstants.AuthTokenHeaderName,
        "Open-Shock-Token",
        "ShockLinkToken"
    ];
    private static readonly string[] DeviceTokenHeaderNames = [
        AuthConstants.DeviceAuthTokenHeaderName,
        "Device-Token"
    ];

    public static void SetSessionKeyCookie(this HttpContext context, string sessionKey, string domain)
    {
        context.Response.Cookies.Append(AuthConstants.SessionCookieName, sessionKey, new CookieOptions
        {
            Expires = new DateTimeOffset(DateTime.UtcNow.Add(Duration.LoginSessionLifetime)),
            Secure = true,
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Domain = domain
        });
    }

    public static void RemoveSessionKeyCookie(this HttpContext context, string domain)
    {
        context.Response.Cookies.Append(AuthConstants.SessionCookieName, string.Empty, new CookieOptions
        {
            Expires = DateTime.Now.AddDays(-1),
            Secure = true,
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Domain = domain
        });
    }

    public static bool TryGetSessionKeyFromCookie(this HttpContext context, [NotNullWhen(true)] out string? sessionKey)
    {
        if (context.Request.Cookies.TryGetValue(AuthConstants.SessionCookieName, out sessionKey) && !string.IsNullOrEmpty(sessionKey))
        {
            return true;
        }

        sessionKey = null;

        return false;
    }

    public static bool TryGetSessionAuthFromHeader(this HttpContext context, [NotNullWhen(true)] out string? sessionKey)
    {
        if (context.Request.Headers.TryGetValue(AuthConstants.SessionHeaderName, out var value) && !string.IsNullOrEmpty(value))
        {
            sessionKey = value!;

            return true;
        }

        sessionKey = null;

        return false;
    }

    public static bool TryGetSessionKey(this HttpContext context, [NotNullWhen(true)] out string? sessionKey)
    {
        if (TryGetSessionKeyFromCookie(context, out sessionKey)) return true;
        if (TryGetSessionAuthFromHeader(context, out sessionKey)) return true;

        sessionKey = null;

        return false;
    }

    public static bool TryGetAuthTokenFromHeader(this HttpContext context, [NotNullWhen(true)] out string? token)
    {
        foreach (string header in TokenHeaderNames)
        {
            if (context.Request.Headers.TryGetValue(header, out var value) && !string.IsNullOrEmpty(value))
            {
                token = value!;

                return true;
            }
        }

        token = null;

        return false;
    }

    public static bool TryGetDeviceTokenFromHeader(this HttpContext context, [NotNullWhen(true)] out string? token)
    {
        foreach (string header in DeviceTokenHeaderNames)
        {
            if (context.Request.Headers.TryGetValue(header, out var value) && !string.IsNullOrEmpty(value))
            {
                token = value!;

                return true;
            }
        }

        token = null;

        return false;
    }

}
