﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Requests;
using OpenShock.API.Utils;
using OpenShock.Common.Models;
using System.Net;

namespace OpenShock.API.Controller.Shares.Links;

public sealed partial class ShareLinksController
{
    /// <summary>
    /// Pause a shocker in a share link
    /// </summary>
    /// <param name="id"></param>
    /// <param name="shockerId"></param>
    /// <param name="data"></param>
    /// <response code="200">Successfully updated paused state shocker</response>
    /// <response code="404">Share link or shocker does not exist</response>
    /// <response code="400">Shocker does not exist in share link</response>
    [HttpPost("{id}/{shockerId}/pause", Name = "PauseShockerShareLink")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<BaseResponse<PauseReason>> PauseShocker([FromRoute] Guid id, [FromRoute] Guid shockerId, [FromBody] PauseRequest data)
    {
        var exists = await _db.ShockerSharesLinks.AnyAsync(x => x.OwnerId == CurrentUser.DbUser.Id && x.Id == id);
        if (!exists)
            return EBaseResponse<PauseReason>("Share link could not be found", HttpStatusCode.NotFound);

        var shocker =
            await _db.ShockerSharesLinksShockers.Where(x =>
                x.ShareLinkId == id && x.ShockerId == shockerId).Include(x => x.Shocker).FirstOrDefaultAsync();
        if (shocker == null)
            return EBaseResponse<PauseReason>("Shocker does not exist in share link");

        shocker.Paused = data.Pause;
        await _db.SaveChangesAsync();

        return new BaseResponse<PauseReason>
        {
            Message = "Successfully updated paused state shocker",
            Data = ShareLinkUtils.GetPausedReason(shocker.Paused, shocker.Shocker.Paused)
        };
    }
}