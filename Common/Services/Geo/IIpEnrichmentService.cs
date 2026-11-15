using System.Net;

namespace OpenShock.Common.Services.Geo;

public interface IIpEnrichmentService
{
    /// <summary>
    /// Returns null when neither GeoLite2 database is configured or available.
    /// </summary>
    IpEnrichmentData? Enrich(IPAddress ip);
}
