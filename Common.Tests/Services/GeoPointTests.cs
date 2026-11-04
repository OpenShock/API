using OpenShock.Common.Services.Geo;

namespace OpenShock.Common.Tests.Services;

public class GeoPointTests
{
    [Test]
    public async Task DistanceToKm_BerlinToParis_MatchesKnownDistance()
    {
        var berlin = new GeoPoint(52.5200, 13.4050);
        var paris = new GeoPoint(48.8566, 2.3522);

        await Assert.That(berlin.DistanceToKm(paris)).IsBetween(870, 885);
    }

    [Test]
    public async Task DistanceToKm_IsSymmetricAndZeroForSamePoint()
    {
        var a = new GeoPoint(47.61, -122.33);
        var b = new GeoPoint(-33.87, 151.21);

        await Assert.That(a.DistanceToKm(a)).IsEqualTo(0);
        await Assert.That(Math.Abs(a.DistanceToKm(b) - b.DistanceToKm(a))).IsLessThan(0.01);
    }

    [Test]
    public async Task DistanceToKm_AcrossAntimeridian_TakesShortWay()
    {
        var west = new GeoPoint(0, 179.5);
        var east = new GeoPoint(0, -179.5);

        await Assert.That(west.DistanceToKm(east)).IsLessThan(120);
    }

    [Test]
    [Arguments(null, 10d)]
    [Arguments(10d, null)]
    [Arguments(91d, 0d)]
    [Arguments(0d, 181d)]
    [Arguments(double.NaN, 0d)]
    public async Task From_RejectsMissingOrInvalid(double? latitude, double? longitude)
    {
        await Assert.That(GeoPoint.From(latitude, longitude)).IsNull();
    }

    [Test]
    public async Task From_AcceptsValid()
    {
        await Assert.That(GeoPoint.From(50.11, 8.68)).IsEqualTo(new GeoPoint(50.11, 8.68));
    }
}
