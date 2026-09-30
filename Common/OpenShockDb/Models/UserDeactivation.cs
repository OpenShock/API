namespace OpenShock.Common.OpenShockDb;

public sealed class UserDeactivation
{
    public required Guid DeactivatedUserId { get; set; }

    public required Guid DeactivatedByUserId { get; set; }

    public required bool DeleteLater { get; set; }

    public Guid? UserModerationId { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigations
    public User DeactivatedUser { get; set; } = null!;
    public User DeactivatedByUser { get; set; } = null!;
}
