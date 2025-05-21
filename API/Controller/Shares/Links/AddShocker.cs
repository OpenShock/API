using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.OpenShockDb;
using System.Net.Mime;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using OpenShock.Common.Models;

namespace OpenShock.API.Controller.Shares.Links;

public sealed partial class ShareLinksController
{
    /// <summary>
    /// Add a shocker to a public share
    /// </summary>
    /// <param name="publicShareId"></param>
    /// <param name="shockerId"></param>
    /// <response code="200">Successfully added shocker</response>
    /// <response code="404">Public share or shocker does not exist</response>
    /// <response code="409">Shocker already exists in public share</response>
    [HttpPost("{publicShareId}/{shockerId}")]
    [ProducesResponseType<LegacyEmptyResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // PublicShareNotFound, ShockerNotFound
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)] // ShockerAlreadyInPublicShare
    public async Task<IActionResult> AddShocker([FromRoute] Guid publicShareId, [FromRoute] Guid shockerId)
    {
        var exists = await _db.PublicShares.AnyAsync(x => x.OwnerId == CurrentUser.Id && x.Id == publicShareId);
        if (!exists) return Problem(PublicShareError.PublicShareNotFound);

        var ownShocker =
            await _db.Shockers.AnyAsync(x => x.Id == shockerId && x.Device.OwnerId == CurrentUser.Id);
        if (!ownShocker) return Problem(ShockerError.ShockerNotFound);

        if (await _db.PublicShareShockerMappings.AnyAsync(x => x.PublicShareId == publicShareId && x.ShockerId == shockerId))
            return Problem(PublicShareError.ShockerAlreadyInPublicShare);

        _db.PublicShareShockerMappings.Add(new PublicShareShocker
        {
            PublicShareId = publicShareId,
            ShockerId = shockerId,
            AllowShock = true,
            AllowVibrate = true,
            AllowSound = true,
            AllowLiveControl = false,
            IsPaused = false
        });

        await _db.SaveChangesAsync();
        
        return LegacyEmptyOk("Successfully added shocker");
    }
}