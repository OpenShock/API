using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.ServicesCommon.Utils;
using ShockLink.API.Models;
using ShockLink.API.Models.Response;
using ShockLink.API.Utils;
using ShockLink.Common.Models;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller.Public.Shares.Links;

[ApiController]
[Route("/{version:apiVersion}/public/shares/links")]
[AllowAnonymous]
public class PublicShareLinkController : ShockLinkControllerBase
{
    private readonly ShockLinkContext _db;

    public PublicShareLinkController(ShockLinkContext db)
    {
        _db = db;
    }

    [HttpGet("{id:guid}")]
    public async Task<BaseResponse<PublicShareLinkResponse>> Get(Guid id)
    {
        var shareLink = await _db.ShockerSharesLinks.Where(x => x.Id == id).Select(x => new
        {
            Author = new GenericIni
            {
                Id = x.Owner.Id,
                Name = x.Owner.Name,
                Image = GravatarUtils.GetImageUrl(x.Owner.Email)
            },
            x.Id,
            x.Name,
            x.ExpiresOn,
            x.CreatedOn,
            Shockers = x.ShockerSharesLinksShockers.Select(y => new
            {
                DeviceId = y.Shocker.DeviceNavigation.Id,
                DeviceName = y.Shocker.DeviceNavigation.Name,
                Shocker = new ShareLinkShocker
                {
                    Id = y.Shocker.Id,
                    Name = y.Shocker.Name,
                    Limits = new ShockerLimits
                    {
                        Duration = y.LimitDuration,
                        Intensity = y.LimitIntensity
                    },
                    Permissions = new ShockerPermissions
                    {
                        Vibrate = y.PermVibrate,
                        Sound = y.PermSound,
                        Shock = y.PermShock,
                    },
                    Paused = ShareLinkUtils.GetPausedReason(y.Paused, y.Shocker.Paused),
                }
            })
        }).SingleOrDefaultAsync();

        if (shareLink == null) return EBaseResponse<PublicShareLinkResponse>("Share link does not exist");


        var final = new PublicShareLinkResponse
        {
            Id = shareLink.Id,
            Name = shareLink.Name,
            Author = shareLink.Author,
            CreatedOn = shareLink.CreatedOn,
            ExpiresOn = shareLink.ExpiresOn
        };
        foreach (var shocker in shareLink.Shockers)
        {
            if (final.Devices.All(x => x.Id != shocker.DeviceId))
                final.Devices.Add(new ShareLinkDevice
                {
                    Id = shocker.DeviceId,
                    Name = shocker.DeviceName,
                });

            final.Devices.Single(x => x.Id == shocker.DeviceId).Shockers.Add(shocker.Shocker);
        }

        return new BaseResponse<PublicShareLinkResponse>
        {
            Data = final
        };
    }


}