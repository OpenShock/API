using OpenShock.Internal.Common.Constants;

namespace OpenShock.Common.Utils;

/// <summary>
/// Clamps control values to a share's caps and the hard limits. A cap outside the hard limits is itself
/// brought into range first, so a bad cap narrows to the nearest valid one instead of making the clamp throw.
/// </summary>
public static class ControlLimitUtils
{
    public static byte ClampIntensity(byte intensity, byte? cap)
    {
        var max = Math.Min(cap ?? HardLimits.MaxControlIntensity, HardLimits.MaxControlIntensity);
        return Math.Clamp(intensity, HardLimits.MinControlIntensity, max);
    }

    public static ushort ClampDuration(ushort duration, ushort? cap)
    {
        var max = Math.Max(cap ?? HardLimits.MaxControlDuration, HardLimits.MinControlDuration);
        return Math.Clamp(duration, HardLimits.MinControlDuration, max);
    }
}
