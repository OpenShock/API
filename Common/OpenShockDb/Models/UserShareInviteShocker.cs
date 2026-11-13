namespace OpenShock.Common.OpenShockDb;

public sealed class UserShareInviteShocker : SafetySettings
{
    public required Guid InviteId { get; init; }

    public required Guid ShockerId { get; init; }

    // Navigations
    public UserShareInvite Invite { get; init; } = null!;
    public Shocker Shocker { get; init; } = null!;
}
