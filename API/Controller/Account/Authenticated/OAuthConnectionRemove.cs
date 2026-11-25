using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        // An account without a password signs in through its connections only. Removing the last one would
        // lock its owner out until they notice and go through a password reset.
        if (string.IsNullOrEmpty(CurrentUser.PasswordHash))
        {
            var providers = await connectionService.GetConnectionsAsNonTrackingQuery(CurrentUser.Id, cancellationToken)
                .Select(c => c.ProviderKey)
                .ToListAsync(cancellationToken);

            if (providers.Count == 1 && string.Equals(providers[0], provider, StringComparison.OrdinalIgnoreCase))
                return Problem(OAuthError.LastLoginMethod);
        }

        var deleted = await connectionService.TryRemoveConnectionAsync(CurrentUser.Id, provider, actorId: CurrentUser.Id, cancellationToken);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
