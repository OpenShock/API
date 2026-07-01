using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Models.Response;

public sealed class AuditLogEntryResponse
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required Guid ActorId { get; init; }
    public required AuditAction Action { get; init; }
    public required string? IpAddress { get; init; }
    public required string? UserAgent { get; init; }
    public required AuditMetadata? Metadata { get; init; }
    public required DateTime CreatedAt { get; init; }
}
