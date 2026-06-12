using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Response;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.Audit;
using OpenShock.Common.Utils.Pagination;

namespace OpenShock.API.Controller.Account.Authenticated;

public sealed partial class AuthenticatedAccountController
{
    /// <summary>
    /// Get the audit log for the current user's account.
    /// </summary>
    /// <param name="pagination">Page, sort, and search parameters.</param>
    /// <param name="auditService"></param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">A page of audit log entries.</response>
    [HttpGet("audit-log")]
    [ProducesResponseType<PagedResult<AuditLogEntryResponse>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public async Task<PagedResult<AuditLogEntryResponse>> GetAuditLog(
        [FromQuery] PaginationQuery pagination,
        [FromServices] IAuditService auditService,
        CancellationToken cancellationToken)
    {
        var paged = await auditService.GetPagedForUserAsync(CurrentUser.Id, pagination, cancellationToken);
        return MapPaged(paged);
    }

    internal static PagedResult<AuditLogEntryResponse> MapPaged(PagedResult<UserAuditLog> paged) => new()
    {
        Items = paged.Items.Select(MapEntry).ToArray(),
        Page = paged.Page,
        PageSize = paged.PageSize,
        TotalCount = paged.TotalCount,
    };

    private static AuditLogEntryResponse MapEntry(UserAuditLog x) => new()
    {
        Id = x.Id,
        UserId = x.UserId,
        ActorId = x.ActorId,
        Action = x.Action,
        IpAddress = x.IpAddress,
        UserAgent = x.UserAgent,
        Metadata = x.Metadata,
        CreatedAt = x.CreatedAt,
    };
}
