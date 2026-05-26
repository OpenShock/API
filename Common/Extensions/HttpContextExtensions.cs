using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;

namespace OpenShock.Common.Extensions;

public static class HttpContextExtensions
{
    private static readonly object BypassedTypesItemKey = new();

    public static bool TryGetBypassTokenFromHeader(this HttpContext context, [NotNullWhen(true)] out string? token)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Request.Headers.TryGetValue(AuthConstants.BypassTokenHeaderName, out var value) && !string.IsNullOrEmpty(value))
        {
            token = value!;
            return true;
        }

        token = null;
        return false;
    }

    /// <summary>
    /// Stores the set of bypass types that the header matched. Called by the bypass middleware.
    /// </summary>
    public static void SetBypassedTypes(this HttpContext context, BypassTokenType types)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Items[BypassedTypesItemKey] = types;
    }

    /// <summary>
    /// Returns true if the request presented a bypass token that grants <paramref name="type"/>.
    /// </summary>
    public static bool IsBypassed(this HttpContext context, BypassTokenType type)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!context.Items.TryGetValue(BypassedTypesItemKey, out var v) || v is not BypassTokenType set) return false;
        return (set & type) == type;
    }

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
