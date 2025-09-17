using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using OpenShock.Common.Constants;

namespace OpenShock.Common.Extensions;

public static class HttpContextExtensions
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

    public static bool TryGetUserSessionToken(this HttpContext context, [NotNullWhen(true)] out string? sessionToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        
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
        ArgumentNullException.ThrowIfNull(context);
        
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
        ArgumentNullException.ThrowIfNull(context);
        
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

    public static string GetAuthenticationMethod(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        var authMethodClaim = context.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.AuthenticationMethod);
        if (authMethodClaim is null)
        {
            throw new Exception("No authentication method claim found, this should not happen and is a bug!");
        }

        return authMethodClaim.Value;
    }
}
