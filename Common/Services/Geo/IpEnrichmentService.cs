using System.Net;
using MaxMind.GeoIP2;
using Microsoft.Extensions.Logging;
using OpenShock.Common.Options;

namespace OpenShock.Common.Services.Geo;

public sealed class IpEnrichmentService : IIpEnrichmentService, IDisposable
{
    private static readonly string[] VpnKeywords =
    [
        "mullvad", "nordvpn", "expressvpn", "protonvpn", "ipvanish", "surfshark",
        "privateinternetaccess", "pia", "hidemyass", "purevpn", "cyberghost",
        "windscribe", "tunnelbear", "hotspot shield", "vyprvpn", "airvpn",
        "perfect privacy", "ivpn", "ovpn",
        // Datacenter / hosting ASNs that VPN exit nodes overwhelmingly use
        "digitalocean", "linode", "akamai", "hetzner", "vultr", "ovh",
        "amazon", "google", "microsoft", "choopa", "m247", "datacamp",
        "frantech", "quadranet", "leaseweb", "serverius", "hostwinds",
        "psychz", "tzulo", "nexeon", "misaka",
    ];

    private readonly DatabaseReader? _asnReader;
    private readonly DatabaseReader? _cityReader;
    private readonly ILogger<IpEnrichmentService> _logger;

    public IpEnrichmentService(GeoOptions options, ILogger<IpEnrichmentService> logger)
    {
        _logger = logger;

        _asnReader = TryOpen(options.AsnDbPath, "ASN");
        _cityReader = TryOpen(options.CityDbPath, "City");
    }

    private DatabaseReader? TryOpen(string? path, string dbName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            _logger.LogInformation("GeoLite2 {DbName} database path not configured, skipping", dbName);
            return null;
        }

        if (!File.Exists(path))
        {
            _logger.LogWarning("GeoLite2 {DbName} database not found at {Path}", dbName, path);
            return null;
        }

        try
        {
            return new DatabaseReader(path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open GeoLite2 {DbName} database at {Path}", dbName, path);
            return null;
        }
    }

    public IpEnrichmentData? Enrich(IPAddress ip)
    {
        if (_asnReader is null && _cityReader is null) return null;

        string? asnOrg = null;
        // Null means "unknown" (no ASN DB, lookup miss, or failure); only a resolved ASN org yields a verdict.
        bool? isVpn = null;

        if (_asnReader is not null)
        {
            try
            {
                if (_asnReader.TryAsn(ip, out var asn) && asn is not null)
                {
                    asnOrg = asn.AutonomousSystemOrganization;
                    if (asnOrg is not null)
                    {
                        var lower = asnOrg.ToLowerInvariant();
                        isVpn = Array.Exists(VpnKeywords, k => lower.Contains(k));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "ASN lookup failed for {Ip}", ip);
            }
        }

        string? countryCode = null;
        string? city = null;

        if (_cityReader is not null)
        {
            try
            {
                if (_cityReader.TryCity(ip, out var cityResponse) && cityResponse is not null)
                {
                    countryCode = cityResponse.Country.IsoCode;
                    city = cityResponse.City.Name;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "City lookup failed for {Ip}", ip);
            }
        }

        return new IpEnrichmentData(asnOrg, isVpn, countryCode, city);
    }

    public void Dispose()
    {
        _asnReader?.Dispose();
        _cityReader?.Dispose();
    }
}
