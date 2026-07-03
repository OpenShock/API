using System.Net;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils.Pagination;

namespace OpenShock.Common.Services.Audit;

public interface IAuditService
{
    /// <summary>
    /// Appends an audit log entry. ActorId defaults to UserId (self-service action).
    /// </summary>
    Task LogAsync(
        Guid userId,
        AuditAction action,
        AuditMetadata? metadata = null,
        Guid? actorId = null,
        CancellationToken cancellationToken = default);

    Task<PagedResult<UserAuditLog>> GetPagedForUserAsync(
        Guid userId,
        PaginationQuery pagination,
        CancellationToken cancellationToken = default);

    Task<PagedResult<UserAuditLog>> GetPagedAsync(
        Guid? userId,
        Guid? actorId,
        PaginationQuery pagination,
        CancellationToken cancellationToken = default);
}
