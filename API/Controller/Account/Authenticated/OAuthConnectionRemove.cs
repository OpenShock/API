using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.OAuth;
using OpenShock.API.Services.OAuthConnection;
using OpenShock.Common.ApiVersioning;
using OpenShock.Common.Problems;
using OpenShock.Internal.Common.Problems;

namespace OpenShock.API.Controller.Account.Authenticated;

public sealed partial class AuthenticatedAccountController
{
    /// <summary>
    /// Remove an existing OAuth connection for the current user.
    /// </summary>
    /// <param name="provider">Provider key (e.g. <c>discord</c>).</param>
    /// <param name="connectionService"></param>
    /// <param name="cancellationToken"></param>
    /// <response code="204">Connection removed.</response>
    /// <response code="404">No connection found for this provider.</response>
    /// <response code="409">The connection is the account's only way to sign in.</response>
    [HttpDelete("connections/{provider}")]
    [ApiVersion(1)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)] // LastLoginMethod
    public async Task<IActionResult> RemoveOAuthConnection([FromRoute] string provider, [FromServices] IOAuthConnectionService connectionService, CancellationToken cancellationToken)
    {
        var result = await connectionService.RemoveConnectionAsync(CurrentUser.Id, provider, actorId: CurrentUser.Id, cancellationToken);

        return result switch
        {
            RemoveConnectionResult.Removed => NoContent(),
            RemoveConnectionResult.LastLoginMethod => Problem(OAuthError.LastLoginMethod),
            _ => NotFound()
        };
    }
}
