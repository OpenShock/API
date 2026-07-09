using System.Net;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils.Pagination;

namespace OpenShock.Common.Services.Audit;

public interface IAuditService
{
    /// <summary>
    /// Appends an audit log entry for <paramref name="userId"/> (the affected account).
    /// <paramref name="actorId"/> is who performed it: null for the system, equal to <paramref name="userId"/>
    /// for self-service, or another user's id for a moderator action.
    /// </summary>
    Task LogAsync(
        Guid userId,
        AuditAction action,
        Guid? actorId,
        AuditMetadata? metadata = null,
        string? reason = null,
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
