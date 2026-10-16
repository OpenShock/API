namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// Rollup of shocker control activity in 15 minute buckets. Unlike <see cref="ShockerControlLog"/> these rows are
/// never trimmed, so they back lifetime statistics. 15 minutes since every time zone's UTC offset is a multiple of it,
/// so local days always start on a bucket boundary (an hour would split them in e.g. Asia/Kolkata, UTC+5:30). One row
/// per owner, shocker, controller, bucket, type and live flag. Live control
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

    /// <summary>Start of the 15 minute UTC bucket this row aggregates, see <see cref="ControlStatDelta.BucketSize"/>.</summary>
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
