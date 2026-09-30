using OpenShock.Common.Models;

namespace OpenShock.Common.OpenShockDb;

public sealed class UserAuditLog
{
    public required Guid Id { get; set; }

    /// <summary>The account being affected.</summary>
    public required Guid UserId { get; set; }

    /// <summary>
    /// Who performed the action: null for the system (automated/no human), equal to <see cref="UserId"/>
    /// for self-service actions, or another user's id for moderator-initiated actions.
    /// </summary>
    public Guid? ActorId { get; set; }

    public required AuditAction Action { get; set; }

    /// <summary>Optional free-text justification, primarily for moderator actions.</summary>
    public string? Reason { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public AuditMetadata? Metadata { get; set; }

    public required DateTime CreatedAt { get; set; }

    // Navigations
    public User User { get; set; } = null!;
    public User? Actor { get; set; }
}
