namespace OpenShock.Common.Options;

public sealed class GeoOptions
{
    public const string SectionName = "Geo";

    /// <summary>
    /// Path to the MaxMind GeoLite2-ASN.mmdb file.
    /// </summary>
    public string? AsnDbPath { get; init; }

    /// <summary>
    /// Path to the MaxMind GeoLite2-City.mmdb file.
    /// </summary>
    public string? CityDbPath { get; init; }
}
