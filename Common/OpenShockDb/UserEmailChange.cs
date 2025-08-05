namespace OpenShock.Common.OpenShockDb;

public sealed class UserEmailChange
{
    public required Guid Id { get; set; }

    public required Guid UserId { get; set; }

    public required string OldEmail { get; set; }

    public required string NewEmail { get; set; }

    public required string SecretHash { get; set; }

    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigations
    public User User { get; set; } = null!;
}
