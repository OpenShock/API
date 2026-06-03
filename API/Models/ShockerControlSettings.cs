using System.ComponentModel.DataAnnotations;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Models;

/// <summary>
/// Per-token shocker control configuration: a toggle plus min/max limits for intensity and duration.
/// </summary>
public sealed class ShockerControlSettings : IValidatableObject
{
    /// <summary>
    /// Whether this token is allowed to send shocker control messages at all.
    /// </summary>
    public bool Enabled { get; init; } = true;

    public required IntensityLimitSettings Intensity { get; init; }

    public required DurationLimitSettings Duration { get; init; }

    /// <summary>
    /// The permissive default: enabled, full-range clamp for both intensity and duration (a no-op).
    /// </summary>
    public static ShockerControlSettings Default => new()
    {
        Enabled = true,
        Intensity = new IntensityLimitSettings
        {
            Min = HardLimits.MinControlIntensity,
            Max = HardLimits.MaxControlIntensity,
            Mode = ControlLimitMode.Clamp
        },
        Duration = new DurationLimitSettings
        {
            Min = HardLimits.MinControlDuration,
            Max = HardLimits.MaxControlDuration,
            Mode = ControlLimitMode.Clamp
        }
    };

    /// <summary>
    /// Maps the flat shocker-control columns of an <see cref="ApiToken"/> onto this DTO.
    /// In-memory only; for EF projections construct the object inline so it can be translated.
    /// </summary>
    public static ShockerControlSettings FromToken(ApiToken token) => new()
    {
        Enabled = token.ShockerControlEnabled,
        Intensity = new IntensityLimitSettings
        {
            Min = token.ShockerControlIntensityMin,
            Max = token.ShockerControlIntensityMax,
            Mode = token.ShockerControlIntensityMode
        },
        Duration = new DurationLimitSettings
        {
            Min = token.ShockerControlDurationMin,
            Max = token.ShockerControlDurationMax,
            Mode = token.ShockerControlDurationMode
        }
    };

    /// <summary>
    /// Writes this configuration onto the flat shocker-control columns of an <see cref="ApiToken"/>.
    /// </summary>
    public void ApplyTo(ApiToken token)
    {
        token.ShockerControlEnabled = Enabled;
        token.ShockerControlIntensityMin = Intensity.Min;
        token.ShockerControlIntensityMax = Intensity.Max;
        token.ShockerControlIntensityMode = Intensity.Mode;
        token.ShockerControlDurationMin = Duration.Min;
        token.ShockerControlDurationMax = Duration.Max;
        token.ShockerControlDurationMode = Duration.Mode;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (Intensity is not null && Intensity.Min > Intensity.Max)
        {
            yield return new ValidationResult(
                "Intensity min must be less than or equal to max",
                [nameof(Intensity)]);
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (Duration is not null && Duration.Min > Duration.Max)
        {
            yield return new ValidationResult(
                "Duration min must be less than or equal to max",
                [nameof(Duration)]);
        }
    }
}

public sealed class IntensityLimitSettings
{
    [Range(HardLimits.MinControlIntensity, HardLimits.MaxControlIntensity)]
    public required byte Min { get; init; }

    [Range(HardLimits.MinControlIntensity, HardLimits.MaxControlIntensity)]
    public required byte Max { get; init; }

    public ControlLimitMode Mode { get; init; } = ControlLimitMode.Clamp;
}

public sealed class DurationLimitSettings
{
    [Range(HardLimits.MinControlDuration, HardLimits.MaxControlDuration)]
    public required ushort Min { get; init; }

    [Range(HardLimits.MinControlDuration, HardLimits.MaxControlDuration)]
    public required ushort Max { get; init; }

    public ControlLimitMode Mode { get; init; } = ControlLimitMode.Clamp;
}
