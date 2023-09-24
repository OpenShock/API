using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShockLink.API.Models;
using ShockLink.API.Models.Requests;
using ShockLink.API.Models.Response;
using ShockLink.API.Utils;
using ShockLink.Common.Models;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller.Shockers;

public sealed partial class ShockerController
{
    [HttpGet("{id:guid}/shares")]
    public async Task<BaseResponse<IEnumerable<ShareInfo>>> GetShares(Guid id)
    {
        var owns = await _db.Shockers.AnyAsync(x => x.DeviceNavigation.Owner == CurrentUser.DbUser.Id && x.Id == id);
        if(!owns) return EBaseResponse<IEnumerable<ShareInfo>>("Device/Shocker does not exists or device does not belong to you",
            HttpStatusCode.NotFound);
        var shares = await _db.ShockerShares
            .Where(x => x.ShockerId == id && x.Shocker.DeviceNavigation.Owner == CurrentUser.DbUser.Id).Select(x =>
                new ShareInfo
                {
                    Paused = x.Paused,
                    SharedWith = new GenericIni
                    {
                        Name = x.SharedWithNavigation.Name,
                        Id = x.SharedWith,
                        Image = GravatarUtils.GetImageUrl(x.SharedWithNavigation.Email)
                    },
                    CreatedOn = x.CreatedOn,
                    Permissions = new ShockerPermissions
                    {
                        Sound = x.PermSound!.Value,
                        Shock = x.PermShock!.Value,
                        Vibrate = x.PermVibrate!.Value
                    },
                    Limits = new ShockerLimits
                    {
                        Duration = x.LimitDuration,
                        Intensity = x.LimitIntensity
                    }
                }
            ).ToListAsync();

        return new BaseResponse<IEnumerable<ShareInfo>>
        {
            Data = shares
        };
    }
    
    [HttpGet("{id:guid}/shareCodes")]
    public async Task<BaseResponse<IEnumerable<ShareCodeInfo>>> GetShareCodes(Guid id)
    {
        var owns = await _db.Shockers.AnyAsync(x => x.DeviceNavigation.Owner == CurrentUser.DbUser.Id && x.Id == id);
        if(!owns) return EBaseResponse<IEnumerable<ShareCodeInfo>>("Device/Shocker does not exists or device does not belong to you",
            HttpStatusCode.NotFound);
        var shares = await _db.ShockerShareCodes
            .Where(x => x.ShockerId == id && x.Shocker.DeviceNavigation.Owner == CurrentUser.DbUser.Id).Select(x =>
                new ShareCodeInfo
                {
                    CreatedOn = x.CreatedOn,
                    Id = x.Id
                }
            ).ToListAsync();

        return new BaseResponse<IEnumerable<ShareCodeInfo>>
        {
            Data = shares
        };
    }
    
    public class ShareCodeInfo
    {
        public required Guid Id { get; set; }
        public required DateTime CreatedOn { get; set; }
    }


    [HttpPost("{id:guid}/shares")]
    public async Task<BaseResponse<Guid>> CreateShareCode(Guid id, CreateShareCode data)
    {
        var device = await _db.Shockers.Where(x => x.DeviceNavigation.Owner == CurrentUser.DbUser.Id && x.Id == id).AnyAsync();
        if (!device)
            return EBaseResponse<Guid>("Device/Shocker does not exists or device does not belong to you",
                HttpStatusCode.NotFound);

        var newCode = new ShockerShareCode
        {
            Id = Guid.NewGuid(),
            ShockerId = id,
            PermVibrate = data.Permissions.Vibrate,
            PermSound = data.Permissions.Sound,
            PermShock = data.Permissions.Shock,
            LimitIntensity = data.Limits.Intensity,
            LimitDuration = data.Limits.Duration
        };
        _db.ShockerShareCodes.Add(newCode);
        await _db.SaveChangesAsync();
        return new BaseResponse<Guid>
        {
            Data = newCode.Id
        };
    }
    
    [HttpDelete("{id:guid}/shares/{sharedWith:guid}")]
    public async Task<BaseResponse<object>> DeleteShare(Guid id, Guid sharedWith)
    {
        var affected = await _db.ShockerShares.Where(x =>
            x.ShockerId == id && x.SharedWith == sharedWith && (x.Shocker.DeviceNavigation.Owner == CurrentUser.DbUser.Id || x.SharedWith == CurrentUser.DbUser.Id)).ExecuteDeleteAsync();
        if (affected <= 0)
            return EBaseResponse<object>("Share does not exists or device/shocker does not belong to you nor is shared with you",
                HttpStatusCode.NotFound);

        return new BaseResponse<object>("Successfully deleted share");
    }
    
    [HttpPatch("{id:guid}/shares/{sharedWith:guid}")]
    public async Task<BaseResponse<object>> UpdateShare(Guid id, Guid sharedWith, CreateShareCode data)
    {
        var affected = await _db.ShockerShares.Where(x =>
            x.ShockerId == id && x.SharedWith == sharedWith && x.Shocker.DeviceNavigation.Owner == CurrentUser.DbUser.Id).SingleOrDefaultAsync();
        if (affected == null)
            return EBaseResponse<object>("Share does not exists or device/shocker does not belong to you",
                HttpStatusCode.NotFound);

        affected.PermShock = data.Permissions.Shock;
        affected.PermSound = data.Permissions.Sound;
        affected.PermVibrate = data.Permissions.Vibrate;
        affected.LimitDuration = data.Limits.Duration;
        affected.LimitIntensity = data.Limits.Intensity;

        await _db.SaveChangesAsync();

        return new BaseResponse<object>("Successfully updated share");
    }
    
    [HttpPost("{id:guid}/shares/{sharedWith:guid}/pause")]
    public async Task<BaseResponse<object>> UpdatePauseShare(Guid id, Guid sharedWith, PauseRequest data)
    {
        var affected = await _db.ShockerShares.Where(x =>
            x.ShockerId == id && x.SharedWith == sharedWith && x.Shocker.DeviceNavigation.Owner == CurrentUser.DbUser.Id).SingleOrDefaultAsync();
        if (affected == null)
            return EBaseResponse<object>("Share does not exists or device/shocker does not belong to you",
                HttpStatusCode.NotFound);

        affected.Paused = data.Pause;

        await _db.SaveChangesAsync();

        return new BaseResponse<object>
        {
            Message = "Successfully updated pause status share",
            Data = data.Pause
        };
    }
}