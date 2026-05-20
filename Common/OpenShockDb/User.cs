using OpenShock.Common.Models;

namespace OpenShock.Common.OpenShockDb;

public sealed class User
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Email { get; set; }

    public string? PasswordHash { get; set; }

    /// <summary>
    /// Monotonically increasing version bumped whenever <see cref="PasswordHash"/> is changed.
    /// Snapshotted onto outstanding password reset requests so that any pending reset becomes
    /// unusable as soon as the password changes through any path.
    /// </summary>
    public int PasswordVersion { get; set; }

    /// <summary>
    /// Monotonically increasing version bumped whenever <see cref="Email"/> is changed.
    /// Snapshotted onto outstanding email change requests so that any pending change becomes
    /// unusable as soon as the email changes through any path.
    /// </summary>
    public int EmailVersion { get; set; }

    public List<RoleType> Roles { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }

    // Navigations
    public UserActivationRequest? UserActivationRequest { get; set; }
    public UserDeactivation? UserDeactivation { get; set; }
    public ICollection<UserOAuthConnection> OAuthConnections { get; set; } = [];
    public ICollection<ApiToken> ApiTokens { get; } = [];
    public ICollection<ApiTokenReport> ReportedApiTokens { get; } = [];
    public ICollection<Device> Devices { get; } = [];
    public ICollection<UserShare> IncomingUserShares { get; } = [];
    public ICollection<UserShareInvite> OutgoingUserShareInvites { get; } = [];
    public ICollection<UserShareInvite> IncomingUserShareInvites { get; } = [];
    public ICollection<PublicShare> OwnedPublicShares { get; } = [];
    public ICollection<ShockerControlLog> ShockerControlLogs { get; } = [];
    public ICollection<UserNameChange> NameChanges { get; } = [];
    public ICollection<UserEmailChange> EmailChanges { get; } = [];
    public ICollection<UserPasswordReset> PasswordResets { get; } = [];
}
