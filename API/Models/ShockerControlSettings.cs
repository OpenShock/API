using System.ComponentModel.DataAnnotations;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

using OpenShock.Internal.Common.Constants;

namespace OpenShock.API.Models;

/// <summary>
/// Per-token shocker control configuration: a toggle plus min/max limits for intensity and duration.
/// </summary>
public sealed class ShockerControlSettings : IValidatableObject
{
    /// <summary>
    /// When true, this token is paused and may not send shocker control messages.
    /// </summary>
    public required bool Paused { get; init; }

    public required IntensityLimitSettings Intensity { get; init; }

    public required DurationLimitSettings Duration { get; init; }

    /// <summary>
    /// Maps the flat shocker-control columns of an <see cref="ApiToken"/> onto this DTO.
    /// In-memory only; for EF projections construct the object inline so it can be translated.
    /// </summary>
    public static ShockerControlSettings FromToken(ApiToken token) => new()
    {
        Paused = token.ShockerControlPaused,
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
        token.ShockerControlPaused = Paused;
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

    public required ControlLimitMode Mode { get; init; }
}

public sealed class DurationLimitSettings
{
    [Range(HardLimits.MinControlDuration, HardLimits.MaxControlDuration)]
    public required ushort Min { get; init; }

    [Range(HardLimits.MinControlDuration, HardLimits.MaxControlDuration)]
    public required ushort Max { get; init; }

    public ControlLimitMode Mode { get; init; }
}
