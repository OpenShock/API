namespace OpenShock.Common.OpenShockDb;

/// <summary>
/// No foreign keys to shockers and controllers, so their stats survive them being deleted.
/// </summary>
public sealed class ShockerControlLog
{
    public required Guid Id { get; set; }

    public required Guid OwnerUserId { get; set; }

    public required Guid ShockerId { get; set; }

    public required Guid? ControlledByUserId { get; set; }

    /// <summary>For live control the burst's average intensity, weighted by duration.</summary>
    public required byte Intensity { get; set; }

    public required byte MaxIntensity { get; set; }

    public required uint Duration { get; set; }

    public required ControlType Type { get; set; }

    public required string? CustomName { get; set; }

    public bool LiveControl { get; set; }

    /// <summary>For live control when the burst started.</summary>
    public required DateTime CreatedAt { get; set; }

    /// <summary>Set by the database. Live control bursts are only written once they end.</summary>
    public DateTime InsertedAt { get; set; }
}
