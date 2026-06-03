using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.Common.Models;

/// <summary>
/// The shocker-control scoping carried by an API token, resolved at control time.
/// </summary>
public readonly struct ApiTokenControlLimits
{
    public required byte IntensityMin { get; init; }
    public required byte IntensityMax { get; init; }
    public required ControlLimitMode IntensityMode { get; init; }

    public required ushort DurationMin { get; init; }
    public required ushort DurationMax { get; init; }
    public required ControlLimitMode DurationMode { get; init; }

    public static ApiTokenControlLimits FromToken(ApiToken token) => new()
    {
        IntensityMin = token.ShockerControlIntensityMin,
        IntensityMax = token.ShockerControlIntensityMax,
        IntensityMode = token.ShockerControlIntensityMode,
        DurationMin = token.ShockerControlDurationMin,
        DurationMax = token.ShockerControlDurationMax,
        DurationMode = token.ShockerControlDurationMode
    };

    /// <summary>
    /// Applies the token's intensity scoping to an incoming control intensity (input range 0-100).
    /// </summary>
    public byte ApplyIntensity(byte intensity) => (byte)Apply(
        intensity, IntensityMin, IntensityMax, IntensityMode,
        HardLimits.MinControlIntensity, HardLimits.MaxControlIntensity);

    /// <summary>
    /// Applies the token's duration scoping to an incoming control duration (input range 300-65535).
    /// </summary>
    public ushort ApplyDuration(ushort duration) => (ushort)Apply(
        duration, DurationMin, DurationMax, DurationMode,
        HardLimits.MinControlDuration, HardLimits.MaxControlDuration);

    private static int Apply(int value, int min, int max, ControlLimitMode mode, int inputMin, int inputMax)
    {
        switch (mode)
        {
            case ControlLimitMode.Lerp:
                var t = inputMax <= inputMin
                    ? 0d
                    : (double)(value - inputMin) / (inputMax - inputMin);
                var lerped = (int)Math.Round(min + (max - min) * t, MidpointRounding.AwayFromZero);
                return Math.Clamp(lerped, min, max);
            case ControlLimitMode.Clamp:
            default:
                return Math.Clamp(value, min, max);
        }
    }
}
