namespace OpenShock.Common.OpenShockDb;

public sealed class PublicShare
{
    public required Guid Id { get; set; }

    public required Guid OwnerId { get; set; }

    public required string Name { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigations
    public User Owner { get; set; } = null!;
    public ICollection<PublicShareShocker> ShockerMappings { get; } = [];
}
