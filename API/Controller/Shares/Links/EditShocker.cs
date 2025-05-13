using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Requests;
using OpenShock.API.Models.Response;
using System.Net.Mime;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using OpenShock.Common.Models;

namespace OpenShock.API.Controller.Shares.Links;

public sealed partial class ShareLinksController
{
    /// <summary>
    /// Edit a shocker in a public share
    /// </summary>
    /// <param name="publicShareId"></param>
    /// <param name="shockerId"></param>
    /// <param name="body"></param>
    /// <response code="200">Successfully updated shocker</response>
    /// <response code="404">Public share or shocker does not exist</response>
    /// <response code="400">Shocker does not exist in public share</response>
    [HttpPatch("{publicShareId}/{shockerId}")]
    [ProducesResponseType<string>(StatusCodes.Status200OK, MediaTypeNames.Text.Plain)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // PublicShareNotFound, ShockerNotInPublicShare
    public async Task<IActionResult> EditShocker([FromRoute] Guid publicShareId, [FromRoute] Guid shockerId, [FromBody] PublicShareEditShocker body)
    {
        var exists = await _db.PublicShares.AnyAsync(x => x.OwnerId == CurrentUser.Id && x.Id == publicShareId);
        if (!exists) return Problem(PublicShareError.PublicShareNotFound);

        var shocker =
            await _db.PublicShareShockers.FirstOrDefaultAsync(x =>
                x.PublicShareId == publicShareId && x.ShockerId == shockerId);
        if (shocker == null) return Problem(PublicShareError.ShockerNotInPublicShare);

        shocker.AllowShock = body.Permissions.Shock;
        shocker.AllowVibrate = body.Permissions.Vibrate;
        shocker.AllowSound = body.Permissions.Sound;
        shocker.AllowLiveControl = body.Permissions.Live;
        shocker.MaxIntensity = body.Limits.Intensity;
        shocker.MaxDuration = body.Limits.Duration;
        shocker.Cooldown = body.Cooldown;

        await _db.SaveChangesAsync();
        return LegacyEmptyOk("Successfully updated shocker");
    }
}