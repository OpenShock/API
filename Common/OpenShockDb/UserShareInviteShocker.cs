namespace OpenShock.Common.OpenShockDb;

public sealed class UserShareInviteShocker : SafetySettings
{
    public required Guid InviteId { get; set; }

    public required Guid ShockerId { get; set; }

    // Navigations
    public UserShareInvite Invite { get; set; } = null!;
    public Shocker Shocker { get; set; } = null!;
}
