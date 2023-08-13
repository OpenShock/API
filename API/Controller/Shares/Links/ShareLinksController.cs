using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShockLink.API.Authentication;
using ShockLink.API.Models;
using ShockLink.API.Models.Requests;
using ShockLink.API.Models.Response;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller.Shares.Links;

[ApiController]
[Route("/{version:apiVersion}/shares/links")]
public class ShareLinksController : AuthenticatedSessionControllerBase
{
    private readonly ShockLinkContext _db;

    public ShareLinksController(ShockLinkContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<BaseResponse<Guid>> CreateShareLink(ShareLinkCreate data)
    {
        var entity = new ShockerSharesLink
        {
            Id = Guid.NewGuid(),
            Owner = CurrentUser.DbUser,
            ExpiresOn = data.ExpiresOn == null ? null : DateTime.SpecifyKind(data.ExpiresOn.Value, DateTimeKind.Utc),
            Name = data.Name
        };
        _db.ShockerSharesLinks.Add(entity);
        await _db.SaveChangesAsync();

        return new BaseResponse<Guid>
        {
            Data = entity.Id
        };
    }

    [HttpDelete("{id:guid}")]
    public async Task<BaseResponse<object>> DeleteShareLink(Guid id)
    {
        var result = await _db.ShockerSharesLinks.Where(x => x.Id == id && x.OwnerId == CurrentUser.DbUser.Id)
            .ExecuteDeleteAsync();

        return result > 0
            ? new BaseResponse<object>("Successfully deleted share link")
            : EBaseResponse<object>("Share link not found or does not belong to you", HttpStatusCode.NotFound);
    }

    [HttpGet]
    public async Task<BaseResponse<IEnumerable<ShareLinkResponse>>> List()
    {
        var ownShareLinks = await _db.ShockerSharesLinks.Where(x => x.OwnerId == CurrentUser.DbUser.Id)
            .Select(x => ShareLinkResponse.GetFromEf(x)).ToListAsync();

        return new BaseResponse<IEnumerable<ShareLinkResponse>>
        {
            Data = ownShareLinks
        };
    }

    [HttpPost("{id:guid}/{shockerId:guid}")]
    public async Task<BaseResponse<object>> AddShocker(Guid id, Guid shockerId)
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

    [HttpPatch("{id:guid}/{shockerId:guid}")]
    public async Task<BaseResponse<ShareLinkResponse>> EditShocker(Guid id, Guid shockerId, ShareLinkEditShocker data)
    {
        var exists = await _db.ShockerSharesLinks.AnyAsync(x => x.OwnerId == CurrentUser.DbUser.Id && x.Id == id);
        if (!exists)
            return EBaseResponse<ShareLinkResponse>("Share link could not be found", HttpStatusCode.NotFound);

        var shocker =
            await _db.ShockerSharesLinksShockers.FirstOrDefaultAsync(x =>
                x.ShareLinkId == id && x.ShockerId == shockerId);
        if (shocker == null)
            return EBaseResponse<ShareLinkResponse>("Shocker does not exist in share link, consider adding a new one");

        shocker.PermSound = data.PermSound;
        shocker.PermVibrate = data.PermVibrate;
        shocker.PermShock = data.PermShock;
        shocker.LimitDuration = data.LimitDuration;
        shocker.LimitIntensity = data.LimitIntensity;
        shocker.Cooldown = data.Cooldown;

        await _db.SaveChangesAsync();
        return new BaseResponse<ShareLinkResponse>
        {
            Message = "Successfully updated shocker"
        };
    }

    [HttpDelete("{id:guid}/{shockerId:guid}")]
    public async Task<BaseResponse<ShareLinkResponse>> DeleteShocker(Guid id, Guid shockerId)
    {
        var exists = await _db.ShockerSharesLinks.AnyAsync(x => x.OwnerId == CurrentUser.DbUser.Id && x.Id == id);
        if (!exists) return EBaseResponse<ShareLinkResponse>("Share link could not be found", HttpStatusCode.NotFound);

        var affected = await _db.ShockerSharesLinksShockers.Where(x => x.ShareLinkId == id && x.ShockerId == shockerId)
            .ExecuteDeleteAsync();
        if (affected > 0)
            return new BaseResponse<ShareLinkResponse>
            {
                Message = "Successfully removed shocker"
            };

        return EBaseResponse<ShareLinkResponse>("Shocker does not exist in share link, consider adding a new one");
    }
}