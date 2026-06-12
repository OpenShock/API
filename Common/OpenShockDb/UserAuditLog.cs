using OpenShock.Common.Models;

namespace OpenShock.Common.OpenShockDb;

public sealed class UserAuditLog
{
    public required Guid Id { get; set; }

    /// <summary>The account being affected.</summary>
    public required Guid UserId { get; set; }

    /// <summary>Who performed the action. Equal to UserId for self-service actions; an admin's Id for admin-initiated actions.</summary>
    public required Guid ActorId { get; set; }

    public required AuditAction Action { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public AuditMetadata? Metadata { get; set; }

    public required DateTime CreatedAt { get; set; }

    // Navigations
    public User User { get; set; } = null!;
    public User Actor { get; set; } = null!;
}
