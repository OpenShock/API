using System.Diagnostics.CodeAnalysis;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Constants;

namespace OpenShock.Common.Extensions;

public static class HttpContextExtensions
{
    // FullName is only null for open generic parameters, which a closed T never is.
    private static string ItemKeyOf<T>() => typeof(T).FullName!;

    /// <summary>
    /// Stashes <paramref name="value"/> in HttpContext.Items under the full name of <typeparamref name="T"/>,
    /// so the writer and every reader agree on the key without a string literal. Used by the
    /// authentication handlers to hand the resolved User, ApiToken and LoginSession to the rest of the
    /// pipeline.
    /// </summary>
    public static void SetItemByType<T>(this HttpContext context, T value) where T : class =>
        context.Items[ItemKeyOf<T>()] = value;

    /// <summary>
    /// The item stashed with <see cref="SetItemByType{T}"/>, or null if none was, e.g. ApiToken on a
    /// session-authenticated request.
    /// </summary>
    public static T? GetItemByType<T>(this HttpContext context) where T : class =>
        context.Items.TryGetValue(ItemKeyOf<T>(), out var value) ? value as T : null;

    /// <summary>
    /// The item stashed with <see cref="SetItemByType{T}"/>; throws if it is missing, which means the
    /// authentication scheme that provides it did not run.
    /// </summary>
    public static T GetRequiredItemByType<T>(this HttpContext context) where T : class =>
        context.GetItemByType<T>() ?? throw new InvalidOperationException(
            $"HttpContext.Items does not contain a required item of type {ItemKeyOf<T>()}.");

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
