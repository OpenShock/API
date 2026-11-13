namespace OpenShock.Common.OpenShockDb;

public sealed class PublicShare
{
    public Guid Id { get; init; }

    public required Guid OwnerId { get; init; }

    public required string Name { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; init; }

    // Navigations
    public User Owner { get; init; } = null!;
    public ICollection<PublicShareShocker> ShockerMappings { get; } = [];
}
