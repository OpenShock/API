namespace OpenShock.Common.OpenShockDb;

public sealed class UserShareInviteShocker : SafetySettings
{
    public required Guid UserShareInviteId { get; set; }

    public required Guid ShockerId { get; set; }

    // Navigations
    public UserShareInvite UserShareInvite { get; set; } = null!;
    public Shocker Shocker { get; set; } = null!;
}
