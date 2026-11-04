using System.Text.Json.Serialization;

namespace OpenShock.Common.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "t")]
[JsonDerivedType(typeof(LoginMetadata), "login")]
[JsonDerivedType(typeof(UsernameChangedMetadata), "usernameChanged")]
[JsonDerivedType(typeof(EmailChangeRequestedMetadata), "emailChangeRequested")]
[JsonDerivedType(typeof(EmailChangedMetadata), "emailChanged")]
[JsonDerivedType(typeof(ApiTokenCreatedMetadata), "apiTokenCreated")]
[JsonDerivedType(typeof(ApiTokenDeletedMetadata), "apiTokenDeleted")]
[JsonDerivedType(typeof(OAuthConnectedMetadata), "oauthConnected")]
[JsonDerivedType(typeof(OAuthDisconnectedMetadata), "oauthDisconnected")]
[JsonDerivedType(typeof(AccountDeactivatedMetadata), "accountDeactivated")]
[JsonDerivedType(typeof(AutomationTokenCreatedMetadata), "automationTokenCreated")]
[JsonDerivedType(typeof(AutomationTokenUpdatedMetadata), "automationTokenUpdated")]
[JsonDerivedType(typeof(AutomationTokenRotatedMetadata), "automationTokenRotated")]
[JsonDerivedType(typeof(AutomationTokenDeletedMetadata), "automationTokenDeleted")]
[JsonDerivedType(typeof(AutomationTokenUsedMetadata), "automationTokenUsed")]
public abstract record AuditMetadata;

public sealed record LoginMetadata(Guid SessionId) : AuditMetadata;

public sealed record UsernameChangedMetadata(string Old, string New) : AuditMetadata;

public sealed record EmailChangeRequestedMetadata(string NewEmail) : AuditMetadata;

public sealed record EmailChangedMetadata(string Old, string New) : AuditMetadata;

public sealed record ApiTokenCreatedMetadata(Guid TokenId, string Name, IReadOnlyList<string> Permissions) : AuditMetadata;

public sealed record ApiTokenDeletedMetadata(Guid TokenId, string Name) : AuditMetadata;

public sealed record OAuthConnectedMetadata(string Provider) : AuditMetadata;

public sealed record OAuthDisconnectedMetadata(string Provider) : AuditMetadata;

public sealed record AccountDeactivatedMetadata(bool DeleteLater) : AuditMetadata;

public sealed record AutomationTokenCreatedMetadata(Guid TokenId, string Name, IReadOnlyList<string> Types, bool AutoCleanupUsers, TimeSpan? AutoCleanupAfter) : AuditMetadata;

/// <summary>The token's settings after the update.</summary>
public sealed record AutomationTokenUpdatedMetadata(Guid TokenId, string Name, IReadOnlyList<string> Types, bool AutoCleanupUsers, TimeSpan? AutoCleanupAfter) : AuditMetadata;

public sealed record AutomationTokenRotatedMetadata(Guid TokenId, string Name) : AuditMetadata;

/// <summary>The token was deleted together with the <see cref="DeletedAccountCount"/> accounts it created.</summary>
public sealed record AutomationTokenDeletedMetadata(Guid TokenId, string Name, int DeletedAccountCount) : AuditMetadata;

/// <summary>An automation token was used on this account. <see cref="Flow"/> is a <c>AutomationTokenFlow</c> name.</summary>
public sealed record AutomationTokenUsedMetadata(Guid TokenId, string Name, string Flow) : AuditMetadata;
