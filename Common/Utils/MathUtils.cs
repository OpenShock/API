namespace OpenShock.Common.Utils;

public static class MathUtils
{
    private const float EarthRadius = 6371f;
    private const float DegToRad = MathF.PI / 180f;
    
    /// <summary>
    /// Calculates the distance between two points on the Earth's surface using the Haversine formula.
    /// </summary>
    /// <param name="lat1"></param>
    /// <param name="lon1"></param>
    /// <param name="lat2"></param>
    /// <param name="lon2"></param>
    /// <returns></returns>
    public static float CalculateHaversineDistance(float lat1, float lon1, float lat2, float lon2)
    {

        float latDist = (lat2 - lat1) * DegToRad;
        float lonDist = (lon2 - lon1) * DegToRad;

        float latVal = MathF.Sin(latDist / 2f);
        float lonVal = MathF.Sin(lonDist / 2f);
        float otherVal = MathF.Cos(lat1 * DegToRad) * MathF.Cos(lat2 * DegToRad);

        float a = latVal * latVal + otherVal * (lonVal * lonVal);
        float b = 2f * MathF.Atan2(MathF.Sqrt(a), MathF.Sqrt(1f - a));

        return EarthRadius * b;
    }
}
