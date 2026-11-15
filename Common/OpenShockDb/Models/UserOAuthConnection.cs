namespace OpenShock.Common.OpenShockDb;

public sealed class UserOAuthConnection
{
    public required Guid UserId { get; init; }

    public required string ProviderKey { get; init; }

    public required string ExternalId { get; init; }

    public required string? DisplayName { get; set; }

    public DateTime CreatedAt { get; init; }

    // Navigations
    public User User { get; init; } = null!;
}
