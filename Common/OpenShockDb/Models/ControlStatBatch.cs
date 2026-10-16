namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// Marks a buffered batch of <see cref="ControlStat"/> deltas as applied, written in the same transaction as the
/// deltas. A batch retried after its commit went through but the acknowledgement got lost is then skipped instead of
/// being counted twice.
/// </summary>
public sealed class ControlStatBatch
{
    public required Guid Id { get; set; }

    public required DateTime AppliedAt { get; set; }
}
