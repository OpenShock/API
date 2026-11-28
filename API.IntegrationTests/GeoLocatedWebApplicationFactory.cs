using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenShock.Common.Services.Geo;

namespace OpenShock.API.IntegrationTests;

/// <summary>
/// Variant of <see cref="WebApplicationFactory"/> whose GeoIP lookup resolves every request to
/// <see cref="ClientLocation"/>, so tests can place the caller without shipping a GeoLite2 database.
/// </summary>
public sealed class GeoLocatedWebApplicationFactory : WebApplicationFactory
{
    private readonly FakeIpEnrichmentService _enrichment = new();

    public GeoPoint? ClientLocation
    {
        get => _enrichment.Location;
        set => _enrichment.Location = value;
    }

    /// <summary>Country every request resolves to, or null for "unknown".</summary>
    public string? ClientCountryCode
    {
        get => _enrichment.CountryCode;
        set => _enrichment.CountryCode = value;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IIpEnrichmentService>();
            services.AddSingleton<IIpEnrichmentService>(_enrichment);
        });
    }

    private sealed class FakeIpEnrichmentService : IIpEnrichmentService
    {
        public GeoPoint? Location { get; set; }
        public string? CountryCode { get; set; }

        public IpEnrichmentData? Enrich(IPAddress ip) =>
            Location is null && CountryCode is null
                ? null
                : new IpEnrichmentData(null, null, null, CountryCode, null, Location, Location is null ? null : 20);
    }
}
