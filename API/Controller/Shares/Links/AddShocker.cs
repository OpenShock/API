using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using System.Net;

namespace OpenShock.API.Controller.Shares.Links;

public sealed partial class ShareLinksController
{
    /// <summary>
    /// Add a shocker to a share link
    /// </summary>
    /// <response code="200">Successfully added shocker</response>
    /// <response code="404">Share link or shocker does not exist</response>
    /// <response code="409">Shocker already exists in share link</response>
    [HttpPost("{id}/{shockerId}", Name = "AddShocker")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Conflict)]
    public async Task<BaseResponse<object>> AddShocker([FromRoute] Guid id, [FromRoute] Guid shockerId)
    {
        var exists = await _db.ShockerSharesLinks.AnyAsync(x => x.OwnerId == CurrentUser.DbUser.Id && x.Id == id);
        if (!exists)
            return EBaseResponse<object>("Share link could not be found", HttpStatusCode.NotFound);

        var ownShocker =
            await _db.Shockers.AnyAsync(x => x.Id == shockerId && x.DeviceNavigation.Owner == CurrentUser.DbUser.Id);
        if (!ownShocker) return EBaseResponse<object>("Shocker does not exist", HttpStatusCode.NotFound);

        if (await _db.ShockerSharesLinksShockers.AnyAsync(x => x.ShareLinkId == id && x.ShockerId == shockerId))
            return EBaseResponse<object>("Shocker already exists in share link", HttpStatusCode.Conflict);

        _db.ShockerSharesLinksShockers.Add(new ShockerSharesLinksShocker
        {
            ShockerId = shockerId,
            ShareLinkId = id,
            PermSound = true,
            PermVibrate = true,
            PermShock = true
        });

        await _db.SaveChangesAsync();
        return new BaseResponse<object>
        {
            Message = "Successfully added shocker"
        };
    }
}