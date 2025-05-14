namespace OpenShock.Common.OpenShockDb;

public sealed class ShockerShare : SafetySettings
{
    public required Guid ShockerId { get; set; }

    public required Guid SharedWithUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigations
    public Shocker Shocker { get; set; } = null!;
    public User SharedWithUser { get; set; } = null!;
}
