namespace OpenShock.Common.OpenShockDb;

public sealed class UserShare : SafetySettings
{
    public required Guid SharedWithUserId { get; init; }

    public required Guid ShockerId { get; set; }
    
    public DateTime CreatedAt { get; init; }

    // Navigations
    public User SharedWithUser { get; init; } = null!;
    public Shocker Shocker { get; init; } = null!;
}
