namespace OpenShock.Common.OpenShockDb;

public sealed class UserNameChange
{
    public int Id { get; init; } // TODO: Make this Guid

    public required Guid UserId { get; init; }

    public required string OldName { get; init; }

    public DateTime CreatedAt { get; init; }

    // Navigations
    public User User { get; init; } = null!;
}
