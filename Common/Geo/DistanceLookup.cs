using OpenShock.Common.Utils;
using System.Collections.Frozen;
using OpenShock.Common.Constants;

namespace OpenShock.Common.Geo;

public static class DistanceLookup
{
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

            // Same country, no need to calculate distance
            yield return new KeyValuePair<int, float>(Alpha2CountryCode.GetCombinedHashCode(first.CountryCode, first.CountryCode), 0f);

            for (int j = i + 1; j < CountryInfo.Countries.Length; j++)
            {
                var second = CountryInfo.Countries[j];

                var id = Alpha2CountryCode.GetCombinedHashCode(first.CountryCode, second.CountryCode);
                var dist = MathUtils.CalculateHaversineDistance(first.Latitude, first.Longitude, second.Latitude, second.Longitude);

                yield return new KeyValuePair<int, float>(id, dist);
            }
        }
    }

    /// <summary>
    /// Stupidly fast lookup for distances between countries
    /// </summary>
    private static readonly FrozenDictionary<int, float> Distances = GetAllDistances().ToFrozenDictionary(); // Create a frozen dictionary for fast lookups

    public static bool TryGetDistanceBetween(Alpha2CountryCode alpha2CountryA, Alpha2CountryCode alpha2CountryB, out float distance)
    {
        if (alpha2CountryA.IsUnknown() || alpha2CountryB.IsUnknown())
        {
            distance = Distance.DistanceToAndromedaGalaxyInKm;

            return false;
        }

        if (!Distances.TryGetValue(Alpha2CountryCode.GetCombinedHashCode(alpha2CountryA, alpha2CountryB), out distance))
        {
            distance = Distance.DistanceToAndromedaGalaxyInKm;

            return false;
        }

        return true;
    }
}