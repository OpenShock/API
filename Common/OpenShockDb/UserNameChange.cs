namespace OpenShock.Common.OpenShockDb;

public sealed class UserNameChange
{
    public int Id { get; set; } // TODO: Make this Guid

    public required Guid UserId { get; set; }

    public required string OldName { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigations
    public User User { get; set; } = null!;
}
