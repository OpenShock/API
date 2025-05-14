using OpenShock.Common.Models;

namespace OpenShock.Common.OpenShockDb;

public sealed class User
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public required bool EmailActivated { get; set; }

    public List<RoleType> Roles { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    // Navigations
    public ICollection<ApiToken> ApiTokens { get; } = [];
    public ICollection<Device> Devices { get; } = [];
    public ICollection<UserShareInvite> OwnedShockerShareRequests { get; } = [];
    public ICollection<UserShareInvite> UserShockerShareRequests { get; } = [];
    public ICollection<PublicShare> OwnedPublicShares { get; } = [];
    public ICollection<ShockerControlLog> ShockerControlLogs { get; } = [];
    public ICollection<UserShare> OwnedUserShares { get; } = [];
    public ICollection<UserShare> ReceivedUserShares { get; } = [];
    public ICollection<UserActivation> UserActivations { get; } = [];
    public ICollection<UserNameChange> NameChanges { get; } = [];
    public ICollection<UserEmailChange> EmailChanges { get; } = [];
    public ICollection<UserPasswordReset> PasswordResets { get; } = [];
}
