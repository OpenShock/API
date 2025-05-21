namespace OpenShock.Common.OpenShockDb;

public sealed class UserShare : SafetySettings
{
    public required Guid SharedWithUserId { get; set; }

    public required Guid ShockerId { get; set; }
    
    public DateTime CreatedAt { get; set; }

    // Navigations
    public User SharedWithUser { get; set; } = null!;
    public Shocker Shocker { get; set; } = null!;
}
