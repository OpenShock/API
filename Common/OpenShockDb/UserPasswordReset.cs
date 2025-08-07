namespace OpenShock.Common.OpenShockDb;

public sealed class UserPasswordReset
{
    public required Guid Id { get; set; }

    public required Guid UserId { get; set; }

    public required string TokenHash { get; set; }

    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigations
    public User User { get; set; } = null!;
}
