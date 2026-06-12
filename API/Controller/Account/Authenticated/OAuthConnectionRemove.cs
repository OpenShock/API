using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Services.OAuthConnection;
using OpenShock.Common.Extensions;
using OpenShock.Common.Utils;
using OpenShock.Common.Models;
using OpenShock.Common.Services.Audit;

namespace OpenShock.API.Controller.Account.Authenticated;

public sealed partial class AuthenticatedAccountController
{
    /// <summary>
    /// Remove an existing OAuth connection for the current user.
    /// </summary>
    /// <param name="provider">Provider key (e.g. <c>discord</c>).</param>
    /// <param name="connectionService"></param>
    /// <param name="auditService"></param>
    /// <param name="cancellationToken"></param>
    /// <response code="204">Connection removed.</response>
    /// <response code="404">No connection found for this provider.</response>
    [HttpDelete("connections/{provider}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveOAuthConnection(
        [FromRoute] string provider,
        [FromServices] IOAuthConnectionService connectionService,
        [FromServices] IAuditService auditService,
        CancellationToken cancellationToken)
    {
        var deleted = await connectionService.TryRemoveConnectionAsync(CurrentUser.Id, provider, cancellationToken);

        if (!deleted)
            return NotFound();

        await auditService.LogAsync(
            CurrentUser.Id,
            AuditAction.OAuthDisconnected,
            ipAddress: HttpContext.GetRemoteIP(),
            userAgent: HttpContext.GetUserAgent(),
            metadata: new OAuthDisconnectedMetadata(provider));

        return NoContent();
    }
}