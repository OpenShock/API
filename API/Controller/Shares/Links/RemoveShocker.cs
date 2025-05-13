using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mime;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using OpenShock.Common.Models;
using OpenShock.Common.Extensions;

namespace OpenShock.API.Controller.Shares.Links;

public sealed partial class ShareLinksController
{
    /// <summary>
    /// Remove a shocker from a public share
    /// </summary>
    /// <param name="publicShareId"></param>
    /// <param name="shockerId"></param>
    /// <response code="200">Successfully removed shocker</response>
    /// <response code="404">Public share or shocker does not exist</response>
    /// <response code="400">Shocker does not exist in public share</response>
    [HttpDelete("{publicShareId}/{shockerId}")]
    [ProducesResponseType<LegacyEmptyResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // PublicShareNotFound, ShockerNotInPublicShare
    public async Task<IActionResult> RemoveShocker([FromRoute] Guid publicShareId, [FromRoute] Guid shockerId)
    {
        var exists = await _db.PublicShares
            .Where(x => x.Id == publicShareId)
            .WhereIsUserOrPrivileged(x => x.Owner, CurrentUser)
            .AnyAsync();
        if (!exists)
        {
            return Problem(PublicShareError.PublicShareNotFound);
        }

        var affected = await _db.PublicShareShockers
            .Where(x => x.PublicShareId == publicShareId && x.ShockerId == shockerId)
            .ExecuteDeleteAsync();
        if (affected <= 0)
        {
            return Problem(PublicShareError.ShockerNotInPublicShare);
        }
        
        return LegacyEmptyOk($"Successfully removed {affected} {(affected == 1 ? "shocker" : "shockers")}");
    }
}