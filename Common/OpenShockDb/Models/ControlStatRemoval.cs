namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// When <see cref="ControlStat"/> rows involving a user, as owner or controller, were last deleted, e.g. by the owner
/// account being deleted. Deletions leave no row behind to carry an <see cref="ControlStat.UpdatedAt"/>, so this is
/// how integrations learn that a user's totals went down. Written by a database trigger, one row per user.
/// </summary>
public sealed class ControlStatRemoval
{
    /// <summary>Deliberately not a foreign key, the user may be the one that got deleted.</summary>
    public required Guid UserId { get; set; }

    public required DateTime RemovedAt { get; set; }
}
