using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Requests;
using OpenShock.API.Models.Response;
using OpenShock.API.Realtime;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis.PubSub;
using OpenShock.ServicesCommon.Utils;

namespace OpenShock.API.Controller.Shockers;

public sealed partial class ShockerController
{
    
    /// <summary>
    /// Get all shares for this shocker
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
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
    
    /// <summary>
    /// Get all share codes for this shocker
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
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


    /// <summary>
    /// Create a new share code for this shocker
    /// </summary>
    /// <param name="id"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    [HttpPost("{id:guid}/shares")]
    public async Task<BaseResponse<Guid>> CreateShareCode(Guid id, CreateShareCode data)
    {
        var device = await _db.Shockers.Where(x => x.DeviceNavigation.Owner == CurrentUser.DbUser.Id && x.Id == id).Select(x => x.Device).SingleOrDefaultAsync();
        if (device == Guid.Empty)
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

        await PubSubManager.SendDeviceUpdate(new DeviceUpdatedMessage
        {
            Id = device
        });
        
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
        
        var device = await _db.Shockers.Where(x => x.DeviceNavigation.Owner == CurrentUser.DbUser.Id && x.Id == id).Select(x => x.Device).SingleOrDefaultAsync();
        await PubSubManager.SendDeviceUpdate(new DeviceUpdatedMessage
        {
            Id = device
        });

        return new BaseResponse<object>("Successfully deleted share");
    }
    
    [HttpPatch("{id:guid}/shares/{sharedWith:guid}")]
    public async Task<BaseResponse<object>> UpdateShare(Guid id, Guid sharedWith, CreateShareCode data)
    {
        var affected = await _db.ShockerShares.Where(x =>
            x.ShockerId == id && x.SharedWith == sharedWith && x.Shocker.DeviceNavigation.Owner == CurrentUser.DbUser.Id).Include(x => x.Shocker).SingleOrDefaultAsync();
        if (affected == null)
            return EBaseResponse<object>("Share does not exists or device/shocker does not belong to you",
                HttpStatusCode.NotFound);

        affected.PermShock = data.Permissions.Shock;
        affected.PermSound = data.Permissions.Sound;
        affected.PermVibrate = data.Permissions.Vibrate;
        affected.LimitDuration = data.Limits.Duration;
        affected.LimitIntensity = data.Limits.Intensity;

        await _db.SaveChangesAsync();
        
        await PubSubManager.SendDeviceUpdate(new DeviceUpdatedMessage
        {
            Id = affected.Shocker.Device
        });

        return new BaseResponse<object>("Successfully updated share");
    }
    
    [HttpPost("{id:guid}/shares/{sharedWith:guid}/pause")]
    public async Task<BaseResponse<object>> UpdatePauseShare(Guid id, Guid sharedWith, PauseRequest data)
    {
        var affected = await _db.ShockerShares.Where(x =>
            x.ShockerId == id && x.SharedWith == sharedWith && x.Shocker.DeviceNavigation.Owner == CurrentUser.DbUser.Id).Include(x => x.Shocker).SingleOrDefaultAsync();
        if (affected == null)
            return EBaseResponse<object>("Share does not exists or device/shocker does not belong to you",
                HttpStatusCode.NotFound);

        affected.Paused = data.Pause;

        await _db.SaveChangesAsync();
        
        await PubSubManager.SendDeviceUpdate(new DeviceUpdatedMessage
        {
            Id = affected.Shocker.Device
        });

        return new BaseResponse<object>
        {
            Message = "Successfully updated pause status share",
            Data = data.Pause
        };
    }
}