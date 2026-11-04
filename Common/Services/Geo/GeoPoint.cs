using OpenShock.Internal.Common.Utils;

namespace OpenShock.Common.Services.Geo;

/// <summary>
/// A WGS84 coordinate in decimal degrees.
/// </summary>
public readonly record struct GeoPoint(double Latitude, double Longitude)
{
    public static bool IsValid(double latitude, double longitude) =>
        double.IsFinite(latitude) && double.IsFinite(longitude)
                                  && latitude is >= -90 and <= 90
                                  && longitude is >= -180 and <= 180;

    /// <summary>
    /// Builds a point when both halves are present and in range, so callers holding two nullable
    /// config or lookup values don't each have to repeat the "both or neither" check.
    /// </summary>
    public static GeoPoint? From(double? latitude, double? longitude) =>
        latitude is { } lat && longitude is { } lon && IsValid(lat, lon) ? new GeoPoint(lat, lon) : null;

    /// <summary>
    /// Great-circle distance in kilometres. Single precision is far finer than the GeoIP data it is fed.
    /// </summary>
    public double DistanceToKm(GeoPoint other) =>
        MathUtils.CalculateHaversineDistance((float)Latitude, (float)Longitude, (float)other.Latitude,
            (float)other.Longitude);
}