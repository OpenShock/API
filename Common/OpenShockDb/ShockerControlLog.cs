using OpenShock.Common.Models;

namespace OpenShock.Common.OpenShockDb;

public sealed class ShockerControlLog
{
    public required Guid Id { get; set; }

    public required Guid ShockerId { get; set; }

    public required Guid? ControlledByUserId { get; set; }

    public required byte Intensity { get; set; }

    public required uint Duration { get; set; }
    
    public required ControlType Type { get; set; }

    public required string? CustomName { get; set; }

    public bool LiveControl { get; set; }

    public required DateTime CreatedAt { get; set; }

    // Navigations
    public Shocker Shocker { get; set; } = null!;
    public User? ControlledByUser { get; set; }
}
