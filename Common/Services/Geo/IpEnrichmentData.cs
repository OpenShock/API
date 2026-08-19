namespace OpenShock.Common.Services.Geo;

public sealed record IpEnrichmentData(
    string? AsnOrg,
    bool? IsVpn,
    string? CountryCode,
    string? City
);
