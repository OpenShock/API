namespace OpenShock.Common.OpenShockDb;

public sealed class ShareRequest
{
    public required Guid Id { get; set; }

    public required Guid OwnerId { get; set; }

    public Guid? UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigations
    public User Owner { get; set; } = null!;
    public User? User { get; set; }
    public ICollection<ShareRequestShocker> ShockerMappings { get; } = [];
}
