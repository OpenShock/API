using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Services.OAuthConnection;

namespace OpenShock.API.Controller.Account.Authenticated;

public sealed partial class AuthenticatedAccountController
{
    /// <summary>
    /// Remove an existing OAuth connection for the current user.
    /// </summary>
    /// <param name="provider">Provider key (e.g. <c>discord</c>).</param>
    /// <param name="connectionService"></param>
    /// <response code="204">Connection removed.</response>
    /// <response code="404">No connection found for this provider.</response>
    [HttpDelete("connections/{provider}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveOAuthConnection([FromRoute] string provider, [FromServices] IOAuthConnectionService connectionService)
    {
        var deleted = await connectionService.TryRemoveConnectionAsync(CurrentUser.Id, provider);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}