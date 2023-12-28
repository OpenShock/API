using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.ServicesCommon.Authentication;
using OpenShock.ServicesCommon.Hubs;

namespace OpenShock.API.Controller.Shares;

public sealed partial class SharesController
{
    /// <summary>
    /// Link a share code to your account
    /// </summary>
    /// <param name="id"></param>
    /// <param name="userHubContext"></param>
    /// <response code="200">Linked share code</response>
    /// <response code="404">Share code not found or does not belong to you</response>
    /// <response code="400">You cannot link your own shocker code / You already have this shocker linked to your account</response>
    /// <response code="500">Error while linking share code to your account</response>
    [HttpPost("code/{id}", Name = "LinkShareCode")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<BaseResponse<object>> LinkCode(
        [FromRoute] Guid id,
        [FromServices] IHubContext<UserHub, IUserHub> userHubContext
    )
    {
        var shareCode = await _db.ShockerShareCodes.Where(x => x.Id == id).Select(x => new
        {
            Share = x, x.Shocker.DeviceNavigation.Owner, x.Shocker.Device
        }).SingleOrDefaultAsync();
        if (shareCode == null) return EBaseResponse<object>("Share code does not exist", HttpStatusCode.NotFound);
        if (shareCode.Owner == CurrentUser.DbUser.Id)
            return EBaseResponse<object>("You cannot link your own shocker code");
        if (await _db.ShockerShares.AnyAsync(x => x.ShockerId == id && x.SharedWith == CurrentUser.DbUser.Id))
            return EBaseResponse<object>("You already have this shocker linked to your account");


        _db.ShockerShares.Add(new ShockerShare
        {
            SharedWith = CurrentUser.DbUser.Id,
            ShockerId = shareCode.Share.ShockerId,
            PermSound = shareCode.Share.PermSound,
            PermVibrate = shareCode.Share.PermVibrate,
            PermShock = shareCode.Share.PermShock,
            LimitDuration = shareCode.Share.LimitDuration,
            LimitIntensity = shareCode.Share.LimitIntensity
        });
        _db.ShockerShareCodes.Remove(shareCode.Share);

        if (await _db.SaveChangesAsync() <= 1)
            return EBaseResponse<object>("Error while linking share code to your account",
                HttpStatusCode.InternalServerError);

        await userHubContext.Clients.Users(shareCode.Owner.ToString(), CurrentUser.DbUser.Id.ToString())
            .DeviceUpdate(shareCode.Device, DeviceUpdateType.ShockerUpdated);

        return new BaseResponse<object>("Successfully linked share code");
    }
}