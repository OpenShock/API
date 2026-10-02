using System.Net;
using Microsoft.Extensions.Logging;

namespace OpenShock.Common.Utils;

/// <summary>
/// Addresses are stored as plain text, so a value that no longer parses is bad data rather than bad input.
/// </summary>
public static class IpAddressUtils
{
    /// <summary>
    /// Parses a stored address, failing the request if it is malformed.
    /// </summary>
    public static IPAddress? ParseStoredOrThrow(string? stored, ILogger logger)
    {
        if (stored is null) return null;

        if (!IPAddress.TryParse(stored, out var address))
        {
            logger.LogError("Stored IP address {StoredIpAddress} is malformed", stored);
            throw new FormatException("Stored IP address is malformed");
        }

        return address;
    }

    /// <summary>
    /// Parses a stored address, dropping a malformed one so the rest of the data stays readable.
    /// </summary>
    public static IPAddress? ParseStoredOrNull(string? stored, ILogger logger)
    {
        if (stored is null) return null;

        if (!IPAddress.TryParse(stored, out var address))
        {
            logger.LogWarning("Stored IP address {StoredIpAddress} is malformed", stored);
            return null;
        }

        return address;
    }
}
