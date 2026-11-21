namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// The <c>control_stats</c> continuous aggregate, <see cref="ShockerControlLog"/>s in 15 minute buckets. Outlives the
/// logs, so it backs lifetime statistics. Stop controls are not counted.
/// </summary>
public sealed class ControlStat
{
    public required DateTime Bucket { get; init; }

    public required Guid OwnerUserId { get; init; }

    public required Guid ShockerId { get; init; }

    /// <summary>Null for public shares / guests.</summary>
    public required Guid? ControllerUserId { get; init; }

    public required ControlType Type { get; init; }

    public required bool Live { get; init; }

    /// <summary>Number of controls, for live control the number of continuous bursts.</summary>
    public required long Count { get; init; }

    public required long DurationMs { get; init; }

    /// <summary>Sum of intensity * duration in ms, divide by <see cref="DurationMs"/> for the weighted average.</summary>
    public required long IntensityMs { get; init; }

    public required byte MaxIntensity { get; init; }
}
