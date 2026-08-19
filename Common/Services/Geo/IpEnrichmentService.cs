using System.Net;
using MaxMind.GeoIP2;
using Microsoft.Extensions.Logging;
using OpenShock.Common.Options;

namespace OpenShock.Common.Services.Geo;

public sealed class IpEnrichmentService : IIpEnrichmentService, IDisposable
{
    // Consumer VPN providers only. Datacenter and hosting ASNs are deliberately NOT listed here:
    // AWS, Google, Microsoft, Akamai, OVH and friends carry an enormous amount of ordinary traffic
    // (corporate egress, mobile carrier NAT, CDN-fronted clients), so treating them as VPN evidence
    // flags a large share of legitimate users. That signal is worth surfacing one day, but as its own
    // "hosting provider" field rather than folded into a flag the UI presents as "VPN".
    //
    // Matched against whole tokens, not raw substrings, so "pia" cannot match "Olympia". Multi-word
    // vendors are listed in both spaced and concatenated form because ASN org names use both.
    private static readonly string[] VpnProviderKeywords =
    [
        "mullvad", "nordvpn", "expressvpn", "protonvpn", "ipvanish", "surfshark",
        "privateinternetaccess", "private internet access", "pia",
        "hidemyass", "hide my ass", "purevpn", "cyberghost",
        "windscribe", "tunnelbear", "hotspot shield", "hotspotshield",
        "vyprvpn", "airvpn", "perfect privacy", "perfectprivacy", "ivpn", "ovpn",
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

    /// <summary>
    /// Whole-token match of an ASN organisation name against <see cref="VpnProviderKeywords"/>.
    /// Punctuation becomes whitespace so "Amazon.com, Inc." tokenises the way a reader expects, and
    /// the padded haystack lets a single Contains express a word-boundary match for phrases too.
    /// </summary>
    public static bool MatchesVpnProvider(string asnOrg)
    {
        var normalized = string.Create(asnOrg.Length, asnOrg, static (span, source) =>
        {
            for (var i = 0; i < source.Length; i++)
            {
                var c = char.ToLowerInvariant(source[i]);
                span[i] = char.IsLetterOrDigit(c) ? c : ' ';
            }
        });

        var haystack = $" {string.Join(' ', normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries))} ";

        return Array.Exists(VpnProviderKeywords, k => haystack.Contains($" {k} ", StringComparison.Ordinal));
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
                        isVpn = MatchesVpnProvider(asnOrg);
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
