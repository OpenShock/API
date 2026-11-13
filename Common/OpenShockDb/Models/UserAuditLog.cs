using OpenShock.Common.Models;

namespace OpenShock.Common.OpenShockDb;

public sealed class UserAuditLog
{
    public required Guid Id { get; init; }

    /// <summary>The account being affected.</summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Who performed the action: null for the system (automated/no human), equal to <see cref="UserId"/>
    /// for self-service actions, or another user's id for moderator-initiated actions.
    /// </summary>
    public Guid? ActorId { get; init; }

    public required AuditAction Action { get; init; }

    /// <summary>Optional free-text justification, primarily for moderator actions.</summary>
    public string? Reason { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public AuditMetadata? Metadata { get; init; }

    public required DateTime CreatedAt { get; init; }

    // Navigations
    public User User { get; init; } = null!;
    public User? Actor { get; init; }
}
