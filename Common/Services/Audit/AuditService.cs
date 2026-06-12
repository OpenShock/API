using System.Net;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils.Pagination;

namespace OpenShock.Common.Services.Audit;

public sealed class AuditService : IAuditService
{
    private static readonly IReadOnlyDictionary<string, SortFunc<UserAuditLog>> Sorters =
        new Dictionary<string, SortFunc<UserAuditLog>>(StringComparer.OrdinalIgnoreCase)
        {
            ["createdAt"] = (q, desc) => desc ? q.OrderByDescending(x => x.CreatedAt) : q.OrderBy(x => x.CreatedAt),
            ["action"] = (q, desc) => desc ? q.OrderByDescending(x => x.Action) : q.OrderBy(x => x.Action),
        };

    private const string DefaultSort = "createdAt";

    private readonly OpenShockContext _db;

    public AuditService(OpenShockContext db)
    {
        _db = db;
    }

    public async Task LogAsync(
        Guid userId,
        AuditAction action,
        IPAddress? ipAddress = null,
        string? userAgent = null,
        AuditMetadata? metadata = null,
        Guid? actorId = null,
        CancellationToken cancellationToken = default)
    {
        _db.UserAuditLogs.Add(new UserAuditLog
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            ActorId = actorId ?? userId,
            Action = action,
            IpAddress = ipAddress?.ToString(),
            UserAgent = userAgent,
            Metadata = metadata,
            CreatedAt = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<PagedResult<UserAuditLog>> GetPagedForUserAsync(
        Guid userId,
        PaginationQuery pagination,
        CancellationToken cancellationToken = default)
    {
        return _db.UserAuditLogs
            .Where(x => x.UserId == userId)
            .ApplySort(pagination, Sorters, DefaultSort)
            .ThenBy(x => x.Id)
            .ToPagedResultAsync(pagination, cancellationToken);
    }

    public Task<PagedResult<UserAuditLog>> GetPagedAsync(
        Guid? userId,
        Guid? actorId,
        PaginationQuery pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _db.UserAuditLogs.AsQueryable();

        if (userId.HasValue) query = query.Where(x => x.UserId == userId.Value);
        if (actorId.HasValue) query = query.Where(x => x.ActorId == actorId.Value);

        return query
            .ApplySort(pagination, Sorters, DefaultSort)
            .ThenBy(x => x.Id)
            .ToPagedResultAsync(pagination, cancellationToken);
    }
}
