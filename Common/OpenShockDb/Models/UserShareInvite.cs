namespace OpenShock.Common.OpenShockDb;

public sealed class UserShareInvite
{
    public Guid Id { get; init; }

    public required Guid OwnerId { get; init; }

    public Guid? RecipientUserId { get; init; }

    public DateTime CreatedAt { get; init; }

    // Navigations
    public User Owner { get; init; } = null!;
    public User? RecipientUser { get; init; }
    public ICollection<UserShareInviteShocker> ShockerMappings { get; } = [];
}
