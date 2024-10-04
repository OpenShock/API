using OpenShock.Common.Geo;
using TUnit.Assertions.Extensions.Numbers;

namespace OpenShock.Common.Tests.Geo;

public class DistanceLookupTests
{
    [Test]
    [Arguments("US", "US", 0f)]
    [Arguments("US", "DE", 7861.5f)]
    public async Task TryGetDistanceBetween_ValidCountries(string str1, string str2, float expectedDistance)
    {
        // Act
        var result = DistanceLookup.TryGetDistanceBetween(str1, str2, out var distance);

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(distance).IsEqualToWithTolerance(expectedDistance, 0.1f);
    }

    [Test]
    [Arguments("US", "XX")]
    [Arguments("XX", "US")]
    [Arguments("XX", "XX")]
    [Arguments("EZ", "PZ")]
    public async Task TryGetDistanceBetween_UnknownCountry(string str1, string str2)
    {
        // Act
        var result = DistanceLookup.TryGetDistanceBetween(str1, str2, out var distance);

        // Assert
        await Assert.That(result).IsFalse();
        await Assert.That(distance).IsEqualTo(0f);
    }
}
