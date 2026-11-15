namespace OpenShock.Common.OpenShockDb;

public sealed class UserDeactivation
{
    public required Guid DeactivatedUserId { get; init; }

    public required Guid DeactivatedByUserId { get; init; }

    public required bool DeleteLater { get; set; }

    public Guid? UserModerationId { get; init; }

    public DateTime CreatedAt { get; init; }

    // Navigations
    public User DeactivatedUser { get; init; } = null!;
    public User DeactivatedByUser { get; init; } = null!;
}
