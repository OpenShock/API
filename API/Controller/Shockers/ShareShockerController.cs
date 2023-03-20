using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShockLink.API.Authentication;
using ShockLink.API.Models;
using ShockLink.API.Models.Requests;
using ShockLink.API.Models.Response;
using ShockLink.API.Utils;
using ShockLink.Common.Models;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller.Shockers;

[ApiController]
[Route("/{version:apiVersion}/shockers")]
public class ShareShockerController : AuthenticatedSessionControllerBase
{
    private readonly ShockLinkContext _db;

    public ShareShockerController(ShockLinkContext db)
    {
        _db = db;
    }

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
                    SharedWith = new GenericIni
                    {
                        Name = x.SharedWithNavigation.Name,
                        Id = x.SharedWith,
                        Image = ImagesApi.GetImage(x.SharedWithNavigation.Image, ImageVariant.x256)
                    },
                    CreatedOn = x.CreatedOn,
                    Permissions = new ShareInfo.PermissionsObj
                    {
                        Sound = x.PermSound,
                        Shock = x.PermShock,
                        Vibrate = x.PermVibrate
                    },
                    Limits = new ShareInfo.LimitObj
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
    public async Task<BaseResponse<Guid>> CreateShare(Guid id, CreateShareCode data)
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
}