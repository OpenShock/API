using System.Diagnostics.CodeAnalysis;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Constants;

namespace OpenShock.Common.Extensions;

public static class HttpContextExtensions
{
    /// <summary>
    /// The API token this request authenticated with, as stashed by ApiTokenAuthentication,
    /// or null for any other scheme.
    /// </summary>
    public static ApiToken? GetApiTokenItem(this HttpContext context) =>
        context.Items.TryGetValue(nameof(ApiToken), out var value) ? value as ApiToken : null;

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

    public static bool TryGetHubTokenFromHeader(this HttpContext context, [NotNullWhen(true)] out string? token)
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
}
