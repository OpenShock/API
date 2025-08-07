namespace OpenShock.Common.OpenShockDb;

public sealed class UserActivationRequest
{
    public required Guid UserId { get; set; }

    public required string TokenHash { get; set; }

    public int EmailSendAttempts { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigations
    public User User { get; set; } = null!;
}
