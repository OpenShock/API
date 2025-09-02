namespace OpenShock.Common.OpenShockDb;

public sealed class OAuthConnection
{
    public required Guid UserId { get; set; }

    public required string OAuthProvider { get; set; }

    public required string OAuthAccountId { get; set; }

    public required string? OAuthAccountName { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigations
    public User User { get; set; } = null!;
}
