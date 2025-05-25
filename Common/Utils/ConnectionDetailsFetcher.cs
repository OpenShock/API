using OpenShock.Common.Geo;
using System.Diagnostics.CodeAnalysis;
using System.Net;
// ReSharper disable InconsistentNaming

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

    public static bool TryGetCFIPCountryCode(this HttpContext context, out Alpha2CountryCode code)
    {
        if (!context.Request.Headers.TryGetValue("CF-IPCountry", out var value))
        {
            code = Alpha2CountryCode.UnknownCountry;
            return false;
        }

        if (value.Count != 1)
        {
            code = Alpha2CountryCode.UnknownCountry;
            return false;
        }

        return Alpha2CountryCode.TryParse(value[0], out code);
    }

    public static string? GetCFIPCountry(this HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("CF-IPCountry", out var value))
        {
            return null;
        }

        return value;
    }
}
