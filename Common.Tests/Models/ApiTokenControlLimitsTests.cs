using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.Common.Tests.Models;

public class ApiTokenControlLimitsTests
{
    private static ApiTokenControlLimits IntensityLimits(byte min, byte max, ControlLimitMode mode) => new()
    {
        IntensityMin = min,
        IntensityMax = max,
        IntensityMode = mode,
        DurationMin = 300,
        DurationMax = 65535,
        DurationMode = ControlLimitMode.Clamp
    };

    private static ApiTokenControlLimits DurationLimits(ushort min, ushort max, ControlLimitMode mode) => new()
    {
        IntensityMin = 0,
        IntensityMax = 100,
        IntensityMode = ControlLimitMode.Clamp,
        DurationMin = min,
        DurationMax = max,
        DurationMode = mode
    };

    // --- Intensity clamp ---

    [Test]
    [Arguments(0, 100, (byte)100, (byte)100)] // full range is a no-op
    [Arguments(0, 50, (byte)100, (byte)50)]   // above max is capped
    [Arguments(0, 50, (byte)30, (byte)30)]    // within range passes through
    [Arguments(20, 80, (byte)0, (byte)20)]    // below min is raised
    public async Task ApplyIntensity_Clamp(int min, int max, byte input, byte expected)
    {
        var result = IntensityLimits((byte)min, (byte)max, ControlLimitMode.Clamp).ApplyIntensity(input);
        await Assert.That(result).IsEqualTo(expected);
    }

    // --- Intensity lerp (input range 0-100 remapped onto [min,max]) ---

    [Test]
    [Arguments(0, 50, (byte)0, (byte)0)]     // input 0 -> min
    [Arguments(0, 50, (byte)100, (byte)50)]  // input 100 -> max
    [Arguments(0, 50, (byte)50, (byte)25)]   // midpoint -> midpoint
    [Arguments(20, 80, (byte)0, (byte)20)]
    [Arguments(20, 80, (byte)100, (byte)80)]
    [Arguments(20, 80, (byte)50, (byte)50)]
    public async Task ApplyIntensity_Lerp(int min, int max, byte input, byte expected)
    {
        var result = IntensityLimits((byte)min, (byte)max, ControlLimitMode.Lerp).ApplyIntensity(input);
        await Assert.That(result).IsEqualTo(expected);
    }

    // --- Duration clamp ---

    [Test]
    [Arguments(300, 65535, (ushort)1000, (ushort)1000)] // full range is a no-op
    [Arguments(300, 1000, (ushort)5000, (ushort)1000)]  // above max is capped
    [Arguments(300, 1000, (ushort)700, (ushort)700)]    // within range passes through
    public async Task ApplyDuration_Clamp(int min, int max, ushort input, ushort expected)
    {
        var result = DurationLimits((ushort)min, (ushort)max, ControlLimitMode.Clamp).ApplyDuration(input);
        await Assert.That(result).IsEqualTo(expected);
    }

    // --- Duration lerp (input range 300-65535 remapped onto [min,max]) ---

    [Test]
    public async Task ApplyDuration_Lerp_Endpoints()
    {
        var limits = DurationLimits(1000, 5000, ControlLimitMode.Lerp);
        await Assert.That(limits.ApplyDuration(300)).IsEqualTo((ushort)1000);   // input min -> min
        await Assert.That(limits.ApplyDuration(65535)).IsEqualTo((ushort)5000); // input max -> max
    }

    [Test]
    public async Task ApplyDuration_Lerp_Midpoint()
    {
        var limits = DurationLimits(1000, 5000, ControlLimitMode.Lerp);
        // midpoint of input range -> midpoint of [1000,5000] == 3000
        var midInput = (ushort)((300 + 65535) / 2);
        await Assert.That(limits.ApplyDuration(midInput)).IsEqualTo((ushort)3000);
    }
}
