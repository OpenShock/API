using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
                Image = ImagesApi.GetImageRoot(x.Owner.Image)
            },
            x.Id,
            x.Name,
            Shockers = x.ShockerSharesLinksShockers.Select(y => new
            {
                DeviceId = y.Shocker.DeviceNavigation.Id,
                DeviceName = y.Shocker.DeviceNavigation.Name,
                Shocker = new OwnerShockerResponse.SharedDevice.SharedShocker()
                {
                    Id = y.Shocker.Id,
                    Name = y.Shocker.Name,
                    IsPaused = y.Shocker.Paused,
                    PermVibrate = y.PermVibrate,
                    PermSound = y.PermSound,
                    PermShock = y.PermShock
                }
            })
        }).SingleOrDefaultAsync();

        if (shareLink == null) return EBaseResponse<PublicShareLinkResponse>("Share link does not exist");


        var final = new PublicShareLinkResponse
        {
            Id = shareLink.Id,
            Name = shareLink.Name,
            Author = shareLink.Author
        };
        foreach (var shocker in shareLink.Shockers)
        {
            if (final.Devices.All(x => x.Id != shocker.DeviceId))
                final.Devices.Add(new OwnerShockerResponse.SharedDevice
                {
                    Id = shocker.DeviceId,
                    Name = shocker.DeviceName,
                });

            final.Devices.Single(x => x.Id == shocker.DeviceId).Shockers.Add(shocker.Shocker);
        }

        return new BaseResponse<PublicShareLinkResponse>()
        {
            Data = final
        };
    }
}