using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace OpenShock.Common.Utils;

public static class ConnectionDetailsFetcher
{
    public static IPAddress GetRemoteIP(this HttpContext context)
    {
        return context.Connection.RemoteIpAddress ?? IPAddress.Loopback; // IPAddress is null under integration testing
    }

    public static string GetUserAgent(this HttpContext context)
    {
        return context.Request.Headers.UserAgent.ToString();
    }

    public static bool TryGetCFIPCountry(this HttpContext context, [NotNullWhen(true)] out string? cfIpCountry)
    {
        if (!context.Request.Headers.TryGetValue("CF-IPCountry", out var value))
        {
            cfIpCountry = null;
            return false;
        }

        if (string.IsNullOrEmpty(value))
        {
            cfIpCountry = null;
            return false;
        }

        cfIpCountry = value.ToString();

        return true;
    }

    public static string? GetCFIPCountry(this HttpContext context)
    {
        if (!TryGetCFIPCountry(context, out var value)) return null;

        return value;
    }
}
