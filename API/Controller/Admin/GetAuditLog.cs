using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Controller.Account.Authenticated;
using OpenShock.API.Models.Response;
using OpenShock.Common.Services.Audit;
using OpenShock.Common.Utils.Pagination;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Get audit log entries across all users. Optionally filter by subject user or actor.
    /// </summary>
    /// <param name="pagination">Page, sort, and search parameters.</param>
    /// <param name="userId">Optional: filter by the subject user (the account that was affected).</param>
    /// <param name="actorId">Optional: filter by the actor (who performed the action).</param>
    /// <param name="auditService"></param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">A page of audit log entries.</response>
    [HttpGet("audit-log")]
    [ProducesResponseType<PagedResult<AuditLogEntryResponse>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public async Task<PagedResult<AuditLogEntryResponse>> GetAdminAuditLog(
        [FromQuery] PaginationQuery pagination,
        [FromQuery] Guid? userId,
        [FromQuery] Guid? actorId,
        [FromServices] IAuditService auditService,
        CancellationToken cancellationToken)
    {
        var paged = await auditService.GetPagedAsync(userId, actorId, pagination, cancellationToken);
        return AuthenticatedAccountController.MapPaged(paged);
    }
}
