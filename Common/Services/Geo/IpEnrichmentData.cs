namespace OpenShock.Common.Services.Geo;

public sealed record IpEnrichmentData(
    long? Asn,
    string? AsnOrg,
    bool? IsVpn,
    string? CountryCode,
    string? City,
    GeoPoint? Location,
    int? LocationAccuracyRadiusKm
);