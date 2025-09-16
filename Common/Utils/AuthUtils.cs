using OpenShock.Common.Constants;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using OpenShock.Common.Authentication;

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

    public static string GetAuthenticationMethod(this HttpContext context)
    {
        var authMethodClaim = context.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.AuthenticationMethod);
        if (authMethodClaim is null)
        {
            throw new Exception("No authentication method claim found, this should not happen and is a bug!");
        }

        return authMethodClaim.Value;
    }
    
    public static bool HasOpenShockUserIdentity(this ClaimsPrincipal user)
    {
        foreach (var ident in user.Identities)
        {
            if (ident is { IsAuthenticated: true, AuthenticationType: OpenShockAuthSchemes.UserSessionCookie })
            {
                return true;
            }
        }

        return false;
    }
    
    public static bool TryGetOpenShockUserIdentity(this ClaimsPrincipal user, [NotNullWhen(true)] out ClaimsIdentity? identity)
    {
        foreach (var ident in user.Identities)
        {
            if (ident is { IsAuthenticated: true, AuthenticationType: OpenShockAuthSchemes.UserSessionCookie })
            {
                identity = ident;
                return true;
            }
        }

        identity = null;
        return false;
    }

    public static bool TryGetAuthenticatedOpenShockUserId(this ClaimsPrincipal user, out Guid userId)
    {
        if (!user.TryGetOpenShockUserIdentity(out var identity))
        {
            userId = Guid.Empty;
            return false;
        }
        
        var idStr = identity.Claims.SingleOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(idStr))
        {
            userId = Guid.Empty;
            return false;
        }

        return Guid.TryParse(idStr, out userId);
    }
}
