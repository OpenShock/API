﻿using OpenShock.Common.Constants;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace OpenShock.Common.Utils;

public static class AuthUtils
{
    private static readonly string[] TokenHeaderNames = [
        AuthConstants.ApiTokenHeaderName,
        "Open-Shock-Token",
        "ShockLinkToken"
    ];
    private static readonly string[] DeviceTokenHeaderNames = [
        AuthConstants.HubTokenHeaderName,
        "Device-Token"
    ];

    private static CookieOptions GetCookieOptions(string domain, TimeSpan lifetime)
    {
        return new CookieOptions
        {
            Expires = new DateTimeOffset(DateTime.UtcNow.Add(lifetime)),
            Secure = true,
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Domain = domain
        };
    }

    public static void SetSessionKeyCookie(this HttpContext context, string sessionKey, string domain)
    {
        context.Response.Cookies.Append(AuthConstants.UserSessionCookieName, sessionKey, GetCookieOptions(domain, Duration.LoginSessionLifetime));
    }

    public static void RemoveSessionKeyCookie(this HttpContext context, string domain)
    {
        context.Response.Cookies.Append(AuthConstants.UserSessionCookieName, string.Empty, GetCookieOptions(domain, TimeSpan.FromDays(-1)));
    }

    public static bool TryGetUserSessionToken(this HttpContext context, [NotNullWhen(true)] out string? sessionToken)
    {
        if (context.Request.Cookies.TryGetValue(AuthConstants.UserSessionCookieName, out sessionToken) && !string.IsNullOrEmpty(sessionToken))
        {
            return true;
        }
        
        if(context.Request.Headers.TryGetValue(AuthConstants.UserSessionHeaderName, out var headerSessionCookie) && !string.IsNullOrEmpty(headerSessionCookie))
        {
            sessionToken = headerSessionCookie.ToString();
            return true;
        }

        sessionToken = null;

        return false;
    }

    public static bool TryGetApiTokenFromHeader(this HttpContext context, [NotNullWhen(true)] out string? token)
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

    public static string[] GetAuthenticationMethods(this HttpContext context)
    {
        return context.User.Claims.Where(x => x.Type == ClaimTypes.AuthenticationMethod).Select(x => x.Value).ToArray();
    }
    
}
