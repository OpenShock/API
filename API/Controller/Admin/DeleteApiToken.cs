using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Services.Token;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Deletes an API token
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpDelete("apitokens/{tokenId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteApiToken([FromRoute] Guid tokenId, [FromServices] IApiTokenService apiTokenService, CancellationToken cancellationToken)
    {
        PedanticallyEnsureAdmin();

        // Deleting straight off the DbSet would skip the audit entry and, more importantly, the token
        // update that tears down an open live control connection - the revoked token would keep
        // controlling devices until that connection closed on its own.
        var deleted = await apiTokenService.DeleteToken(tokenId, actorId: CurrentUser.Id, cancellationToken: cancellationToken);

        return deleted ? Ok() : NotFound();
    }
}