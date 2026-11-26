using OpenShock.Common.Utils;

namespace OpenShock.Common.Tests.Utils;

public sealed class ControlLimitUtilsTests
{
    [Test]
    [Arguments((ushort)1000, null, (ushort)1000)]        // no cap
    [Arguments((ushort)65535, null, (ushort)65535)]      // no cap, hard maximum
    [Arguments((ushort)100, null, (ushort)300)]          // below the hard minimum is raised
    [Arguments((ushort)5000, 2000, (ushort)2000)]        // above the cap is lowered
    [Arguments((ushort)1000, 2000, (ushort)1000)]        // within the cap passes through
    [Arguments((ushort)5000, 0, (ushort)300)]            // a cap below the hard minimum acts as the minimum
    [Arguments((ushort)5000, 299, (ushort)300)]
    public async Task ClampDuration(ushort duration, int? cap, ushort expected)
    {
        await Assert.That(ControlLimitUtils.ClampDuration(duration, (ushort?)cap)).IsEqualTo(expected);
    }

    [Test]
    [Arguments((byte)50, null, (byte)50)]     // no cap
    [Arguments((byte)200, null, (byte)100)]   // above the hard maximum is lowered
    [Arguments((byte)80, 40, (byte)40)]       // above the cap is lowered
    [Arguments((byte)20, 40, (byte)20)]       // within the cap passes through
    [Arguments((byte)200, 250, (byte)100)]    // a cap above the hard maximum acts as the maximum
    public async Task ClampIntensity(byte intensity, int? cap, byte expected)
    {
        await Assert.That(ControlLimitUtils.ClampIntensity(intensity, (byte?)cap)).IsEqualTo(expected);
    }
}
