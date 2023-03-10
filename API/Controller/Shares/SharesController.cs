using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ShockLink.API.Authentication;
using ShockLink.API.Models;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller.Shares;

[ApiController]
[Route("/{version:apiVersion}/shares")]
public class SharesController : AuthenticatedSessionControllerBase
{
    private readonly ShockLinkContext _db;

    public SharesController(ShockLinkContext db)
    {
        _db = db;
    }
    
    [HttpDelete("{id:guid}")]
    public async Task<BaseResponse<object>> DeleteShare(Guid id)
    {
        
        var affected = await _db.ShockerShares.Where(x =>
            x.ShockerId == id && x.SharedWith == CurrentUser.DbUser.Id && x.Shocker.DeviceNavigation.Owner == CurrentUser.DbUser.Id).ExecuteDeleteAsync();
        if (affected <= 0)
            return EBaseResponse<object>("Share does not exists or device/shocker does not belong to you nor is shared with you",
                HttpStatusCode.NotFound);

        return new BaseResponse<object>("Successfully deleted share");
    }

    [HttpDelete("code/{id:guid}")]
    public async Task<BaseResponse<object>> DeleteCode(Guid id)
    {
        var affected = await _db.ShockerShareCodes.Where(x =>
            x.Id == id && x.Shocker.DeviceNavigation.Owner == CurrentUser.DbUser.Id).ExecuteDeleteAsync();
        if (affected <= 0)
            return EBaseResponse<object>("Share code does not exists or device/shocker does not belong to you",
                HttpStatusCode.NotFound);

        return new BaseResponse<object>("Successfully deleted share code");
    }

    [HttpPost("code/{id:guid}")]
    public async Task<BaseResponse<object>> LinkCode(Guid id)
    {
        var shareCode = await _db.ShockerShareCodes.Where(x => x.Id == id).Select(x => new
        {
            Share = x, x.Shocker.DeviceNavigation.Owner
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
        
        return new BaseResponse<object>("Successfully linked share code");
    }

}