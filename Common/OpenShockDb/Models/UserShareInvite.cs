namespace OpenShock.Common.OpenShockDb;

public sealed class UserShareInvite
{
    public required Guid Id { get; set; }

    public required Guid OwnerId { get; set; }

    public Guid? RecipientUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigations
    public User Owner { get; set; } = null!;
    public User? RecipientUser { get; set; }
    public ICollection<UserShareInviteShocker> ShockerMappings { get; } = [];
}
