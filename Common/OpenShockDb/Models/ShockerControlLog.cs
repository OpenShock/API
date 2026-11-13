namespace OpenShock.Common.OpenShockDb;

public sealed class ShockerControlLog
{
    public required Guid Id { get; init; }

    public required Guid ShockerId { get; init; }

    public required Guid? ControlledByUserId { get; init; }

    public required byte Intensity { get; init; }

    public required uint Duration { get; init; }
    
    public required ControlType Type { get; init; }

    public required string? CustomName { get; init; }

    public bool LiveControl { get; init; }

    public required DateTime CreatedAt { get; init; }

    // Navigations
    public Shocker Shocker { get; init; } = null!;
    public User? ControlledByUser { get; init; }
}
