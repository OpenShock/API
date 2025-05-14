namespace OpenShock.Common.OpenShockDb;

public sealed class UserEmailChange
{
    public required Guid Id { get; set; }

    public required Guid UserId { get; set; }

    public string Email { get; set; } = null!;

    public string SecretHash { get; set; } = null!;

    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigations
    public User User { get; set; } = null!;
}
