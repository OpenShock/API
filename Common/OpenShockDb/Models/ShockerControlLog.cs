namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// No foreign keys to shockers and controllers, so their stats survive them being deleted.
/// </summary>
public sealed class ShockerControlLog
{
    public required Guid Id { get; init; }

    public required Guid OwnerUserId { get; init; }

    public required Guid ShockerId { get; init; }

    public required Guid? ControlledByUserId { get; init; }

    /// <summary>For live control the burst's average intensity, weighted by duration.</summary>
    public required byte Intensity { get; init; }

    public required byte MaxIntensity { get; init; }

    public required uint Duration { get; init; }

    public required ControlType Type { get; init; }

    public required string? CustomName { get; init; }

    public bool LiveControl { get; init; }

    /// <summary>For live control when the burst started.</summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>Set by the database. Live control bursts are only written once they end.</summary>
    public DateTime InsertedAt { get; init; }
}
