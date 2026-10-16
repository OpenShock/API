namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// Hourly rollup of shocker control activity. Unlike <see cref="ShockerControlLog"/> these rows are never trimmed,
/// so they back lifetime statistics. One row per owner, shocker, controller, hour, type and live flag. Live control
/// activity is only counted once the gateway has written it to the hub's websocket.
/// </summary>
public sealed class ControlStat
{
    public required Guid Id { get; set; }

    /// <summary>The user who owns the shocker (the one being controlled).</summary>
    public required Guid OwnerUserId { get; set; }

    /// <summary>Null once the shocker has been deleted.</summary>
    public Guid? ShockerId { get; set; }

    /// <summary>
    /// The user who sent the control. Null for public shares / guests. Deliberately not a foreign key, so stats
    /// of deleted users keep their (now dangling) id instead of colliding with the guest bucket.
    /// </summary>
    public Guid? ControllerUserId { get; set; }

    /// <summary>Start of the UTC hour this row aggregates.</summary>
    public required DateTime Bucket { get; set; }

    public required ControlType Type { get; set; }

    /// <summary>Whether this row aggregates live control (gateway) activity.</summary>
    public required bool Live { get; set; }

    /// <summary>Number of controls, for live control the number of continuous bursts.</summary>
    public required long Count { get; set; }

    public required long DurationMs { get; set; }

    /// <summary>Sum of intensity * duration in ms, divide by <see cref="DurationMs"/> for the weighted average.</summary>
    public required long IntensityMs { get; set; }

    public required byte MaxIntensity { get; set; }

    /// <summary>When this row was last added to, change detection for integrations keys off this.</summary>
    public required DateTime UpdatedAt { get; set; }

    // Navigations
    public User Owner { get; set; } = null!;
    public Shocker? Shocker { get; set; }
}
