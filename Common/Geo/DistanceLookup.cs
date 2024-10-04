using OpenShock.Common.Utils;
using System.Collections.Frozen;

namespace OpenShock.Common.Geo;

public static class DistanceLookup
{
    /// <summary>
    /// Generates a unique ID for a pair of countries, regardless of order
    /// </summary>
    /// <param name="code1"></param>
    /// <param name="code2"></param>
    /// <returns></returns>
    private static int CreateId(Alpha2CountryCode code1, Alpha2CountryCode code2)
    {
        int a = (code1.Char1 << 8) | code1.Char2;
        int b = (code2.Char1 << 8) | code2.Char2;

        int v = (a << 16) | b;

        if (a > b) v = int.RotateLeft(v, 16);

        return v;
    }

    /// <summary>
    /// Generates all distances between all countries in the world, along with their unique ID
    /// 
    /// Generates approximately 22k entries
    /// </summary>
    /// <returns></returns>
    private static IEnumerable<KeyValuePair<int, float>> GetAllDistances()
    {
        for (int i = 0; i < CountryInfo.Countries.Length; i++)
        {
            var first = CountryInfo.Countries[i];

            for (int j = i; j < CountryInfo.Countries.Length; j++)
            {
                var second = CountryInfo.Countries[j];

                var id = CreateId(first.CountryCode, second.CountryCode);
                var dist = MathUtils.CalculateHaversineDistance(first.Latitude, first.Longitude, second.Latitude, second.Longitude);

                yield return new KeyValuePair<int, float>(id, dist);
            }
        }
    }

    /// <summary>
    /// Stupidly fast lookup for distances between countries
    /// </summary>
    private static readonly FrozenDictionary<int, float> Distances = GetAllDistances().ToFrozenDictionary(); // Create a frozen dictionary for fast lookups

    public static bool TryGetDistanceBetween(Alpha2CountryCode alpha2CountryA, Alpha2CountryCode alpha2CountryB, out float distance) =>
        Distances.TryGetValue(CreateId(alpha2CountryA, alpha2CountryB), out distance);
}