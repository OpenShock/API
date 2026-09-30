namespace OpenShock.Common.OpenShockDb;

public sealed class UserOAuthConnection
{
    public required Guid UserId { get; set; }

    public required string ProviderKey { get; set; }

    public required string ExternalId { get; set; }

    public required string? DisplayName { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigations
    public User User { get; set; } = null!;
}
