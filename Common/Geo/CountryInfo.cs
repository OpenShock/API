namespace OpenShock.Common.Geo;

public sealed record CountryInfo(Alpha2CountryCode CountryCode, string Name, float Latitude, float Longitude, string? CfRegion)
{
    public static readonly CountryInfo UnknownCountry = new(Alpha2CountryCode.UnknownCountryCode, "Unknown", 0f, 0f, null);
}
