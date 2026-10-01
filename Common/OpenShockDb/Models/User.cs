namespace OpenShock.Common.OpenShockDb;

public sealed class User
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Email { get; set; }

    public string? PasswordHash { get; set; }

    /// <summary>
    /// Opaque value rotated whenever the user's password or email <em>value</em> changes
    /// (re-hashing the same password with a stronger algorithm does not rotate it).
    /// Snapshotted onto pending password resets and email changes so that any rotation
    /// silently invalidates every outstanding request for this user, regardless of which
    /// code path applied the change.
    /// </summary>
    public Guid SecurityStamp { get; set; }

    public List<RoleType> Roles { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }

    /// <summary>
    /// The admin-issued automation token that created this account, which makes it an automated (bot)
    /// account rather than one a person signed up for; these are mainly used by automated tests.
    /// The account is deleted together with the token.
    /// </summary>
    public Guid? CreatedByAutomationTokenId { get; set; }

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
    public ICollection<UserAuditLog> AuditLogs { get; } = [];
    public ICollection<UserAuditLog> ActorAuditLogs { get; } = [];
    public AutomationToken? CreatedByAutomationToken { get; set; }
}
