using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using OpenShock.Common.Constants;
using OpenShock.Common.Services.Bypass;

namespace OpenShock.Common.Extensions;

public static class HttpContextExtensions
{
    private static readonly object ResolvedBypassTokenItemKey = new();

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
    /// Stores the result of resolving the bypass-token header. Called by the bypass middleware.
    /// </summary>
    public static void SetResolvedBypassToken(this HttpContext context, ResolvedBypassToken? resolved)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Items[ResolvedBypassTokenItemKey] = resolved;
    }

    /// <summary>
    /// Returns the bypass token resolved earlier in the pipeline, or <c>null</c> if no header was
    /// present (or it did not resolve to a known token).
    /// </summary>
    public static ResolvedBypassToken? GetResolvedBypassToken(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Items.TryGetValue(ResolvedBypassTokenItemKey, out var v) ? v as ResolvedBypassToken : null;
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
