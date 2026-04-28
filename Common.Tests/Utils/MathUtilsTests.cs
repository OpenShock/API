using OpenShock.Common.Utils;

namespace OpenShock.Common.Tests.Utils;

public class MathUtilsTests
{
    [Test]
    public async Task SamePoint_ReturnsZero()
    {
        var result = MathUtils.CalculateHaversineDistance(0f, 0f, 0f, 0f);
        await Assert.That(result).IsEqualTo(0f);
    }

    [Test]
    public async Task SameCoordinates_ReturnsZero()
    {
        var result = MathUtils.CalculateHaversineDistance(52.52f, 13.405f, 52.52f, 13.405f);
        await Assert.That(result).IsEqualTo(0f);
    }

    [Test]
    public async Task NewYork_To_London_ApproximatelyCorrect()
    {
        // NYC: 40.7128, -74.0060  London: 51.5074, -0.1278
        // Expected: ~5570 km
        var result = MathUtils.CalculateHaversineDistance(40.7128f, -74.006f, 51.5074f, -0.1278f);
        await Assert.That(result).IsGreaterThan(5500f);
        await Assert.That(result).IsLessThan(5650f);
    }

    [Test]
    public async Task NorthPole_To_SouthPole_ApproximatelyHalfCircumference()
    {
        // ~20015 km
        var result = MathUtils.CalculateHaversineDistance(90f, 0f, -90f, 0f);
        await Assert.That(result).IsGreaterThan(19900f);
        await Assert.That(result).IsLessThan(20100f);
    }

    [Test]
    public async Task Equator_QuarterWayAround_ApproximatelyCorrect()
    {
        // 0,0 to 0,90 — quarter circumference at equator ~10008 km
        var result = MathUtils.CalculateHaversineDistance(0f, 0f, 0f, 90f);
        await Assert.That(result).IsGreaterThan(9900f);
        await Assert.That(result).IsLessThan(10100f);
    }

    [Test]
    public async Task IsSymmetric()
    {
        var ab = MathUtils.CalculateHaversineDistance(48.8566f, 2.3522f, 35.6762f, 139.6503f);
        var ba = MathUtils.CalculateHaversineDistance(35.6762f, 139.6503f, 48.8566f, 2.3522f);
        await Assert.That(MathF.Abs(ab - ba)).IsLessThan(0.01f);
    }

    [Test]
    public async Task AntipodalPoints_ApproximatelyHalfCircumference()
    {
        // 0,0 to 0,180 — half circumference ~20015 km
        var result = MathUtils.CalculateHaversineDistance(0f, 0f, 0f, 180f);
        await Assert.That(result).IsGreaterThan(19900f);
        await Assert.That(result).IsLessThan(20100f);
    }
}
