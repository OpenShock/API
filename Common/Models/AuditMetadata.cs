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
