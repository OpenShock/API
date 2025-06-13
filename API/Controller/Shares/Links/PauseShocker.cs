using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Requests;
using OpenShock.API.Utils;
using OpenShock.Common.Models;
using System.Net.Mime;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;

namespace OpenShock.API.Controller.Shares.Links;

public sealed partial class ShareLinksController
{
    /// <summary>
    /// Pause a shocker in a public share
    /// </summary>
    /// <param name="publicShareId"></param>
    /// <param name="shockerId"></param>
    /// <param name="body"></param>
    /// <response code="200">Successfully updated paused state shocker</response>
    /// <response code="404">Public share or shocker does not exist</response>
    /// <response code="400">Shocker does not exist in public share</response>
    [HttpPost("{publicShareId}/{shockerId}/pause")]
    [ProducesResponseType<LegacyDataResponse<PauseReason>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // PublicShareNotFound, ShockerNotInPublicShare
    public async Task<IActionResult> PauseShocker([FromRoute] Guid publicShareId, [FromRoute] Guid shockerId, [FromBody] PauseRequest body)
    {
        var exists = await _db.PublicShares.AnyAsync(x => x.OwnerId == CurrentUser.Id && x.Id == publicShareId);
        if (!exists) return Problem(PublicShareError.PublicShareNotFound);

        var shocker =
            await _db.PublicShareShockerMappings.Where(x =>
                x.PublicShareId == publicShareId && x.ShockerId == shockerId).Include(x => x.Shocker).FirstOrDefaultAsync();
        if (shocker is null) return Problem(PublicShareError.ShockerNotInPublicShare);

        shocker.IsPaused = body.Pause;
        await _db.SaveChangesAsync();

        return LegacyDataOk(PublicShareUtils.GetPausedReason(shocker.IsPaused, shocker.Shocker.IsPaused));
    }
}