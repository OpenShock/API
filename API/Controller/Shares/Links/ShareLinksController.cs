using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Requests;
using OpenShock.API.Models.Response;
using OpenShock.API.Utils;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.ServicesCommon.Authentication;

namespace OpenShock.API.Controller.Shares.Links;

/// <summary>
/// Share links management
/// </summary>
[ApiController]
[Route("/{version:apiVersion}/shares/links")]
public class ShareLinksController : AuthenticatedSessionControllerBase
{
    private readonly OpenShockContext _db;

    public ShareLinksController(OpenShockContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Creates a new share link
    /// </summary>
    /// <param name="data"></param>
    /// <response code="200">The created share link</response>
    [HttpPost(Name = "CreateShareLink")]
    [ProducesResponseType((int) HttpStatusCode.OK)]
    public async Task<BaseResponse<Guid>> CreateShareLink([FromBody] ShareLinkCreate data)
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

    /// <summary>
    /// Deletes a share link
    /// </summary>
    /// <param name="id"></param>
    /// <response code="200">Deleted share link</response>
    /// <response code="404">Share link not found or does not belong to you</response>
    [HttpDelete("{id}", Name = "DeleteShareLink")]
    [ProducesResponseType((int) HttpStatusCode.OK)]
    [ProducesResponseType((int) HttpStatusCode.NotFound)]
    public async Task<BaseResponse<object>> DeleteShareLink([FromRoute] Guid id)
    {
        var result = await _db.ShockerSharesLinks.Where(x => x.Id == id && x.OwnerId == CurrentUser.DbUser.Id)
            .ExecuteDeleteAsync();

        return result > 0
            ? new BaseResponse<object>("Successfully deleted share link")
            : EBaseResponse<object>("Share link not found or does not belong to you", HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Get all share links for the current user
    /// </summary>
    /// <response code="200">All share links for the current user</response>
    [HttpGet(Name = "ListShareLinks")]
    [ProducesResponseType((int) HttpStatusCode.OK)]
    public async Task<BaseResponse<IEnumerable<ShareLinkResponse>>> List()
    {
        var ownShareLinks = await _db.ShockerSharesLinks.Where(x => x.OwnerId == CurrentUser.DbUser.Id)
            .Select(x => ShareLinkResponse.GetFromEf(x)).ToListAsync();

        return new BaseResponse<IEnumerable<ShareLinkResponse>>
        {
            Data = ownShareLinks
        };
    }

    /// <summary>
    /// Add a shocker to a share link
    /// </summary>
    /// <response code="200">Successfully added shocker</response>
    /// <response code="404">Share link or shocker does not exist</response>
    /// <response code="409">Shocker already exists in share link</response>
    [HttpPost("{id}/{shockerId}", Name = "AddShocker")]
    [ProducesResponseType((int) HttpStatusCode.OK)]
    [ProducesResponseType((int) HttpStatusCode.NotFound)]
    [ProducesResponseType((int) HttpStatusCode.Conflict)]
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

    /// <summary>
    /// Edit a shocker in a share link
    /// </summary>
    /// <param name="id"></param>
    /// <param name="shockerId"></param>
    /// <param name="data"></param>
    /// <response code="200">Successfully updated shocker</response>
    /// <response code="404">Share link or shocker does not exist</response>
    /// <response code="400">Shocker does not exist in share link</response>
    [HttpPatch("{id}/{shockerId}", Name = "EditShockerShareLink")]
    [ProducesResponseType((int) HttpStatusCode.OK)]
    [ProducesResponseType((int) HttpStatusCode.NotFound)]
    [ProducesResponseType((int) HttpStatusCode.BadRequest)]
    public async Task<BaseResponse<ShareLinkResponse>> EditShocker([FromRoute] Guid id, [FromRoute] Guid shockerId, [FromBody] ShareLinkEditShocker data)
    {
        var exists = await _db.ShockerSharesLinks.AnyAsync(x => x.OwnerId == CurrentUser.DbUser.Id && x.Id == id);
        if (!exists)
            return EBaseResponse<ShareLinkResponse>("Share link could not be found", HttpStatusCode.NotFound);

        var shocker =
            await _db.ShockerSharesLinksShockers.FirstOrDefaultAsync(x =>
                x.ShareLinkId == id && x.ShockerId == shockerId);
        if (shocker == null)
            return EBaseResponse<ShareLinkResponse>("Shocker does not exist in share link, consider adding a new one");

        shocker.PermSound = data.Permissions.Sound;
        shocker.PermVibrate = data.Permissions.Vibrate;
        shocker.PermShock = data.Permissions.Shock;
        shocker.LimitDuration = data.Limits.Duration;
        shocker.LimitIntensity = data.Limits.Intensity;
        shocker.Cooldown = data.Cooldown;

        await _db.SaveChangesAsync();
        return new BaseResponse<ShareLinkResponse>
        {
            Message = "Successfully updated shocker"
        };
    }

    /// <summary>
    /// Delete a shocker from a share link
    /// </summary>
    /// <param name="id"></param>
    /// <param name="shockerId"></param>
    /// <response code="200">Successfully removed shocker</response>
    /// <response code="404">Share link or shocker does not exist</response>
    /// <response code="400">Shocker does not exist in share link</response>
    [HttpDelete("{id}/{shockerId}", Name = "DeleteShockerShareLink")]
    [ProducesResponseType((int) HttpStatusCode.OK)]
    [ProducesResponseType((int) HttpStatusCode.NotFound)]
    public async Task<BaseResponse<ShareLinkResponse>> DeleteShocker([FromRoute] Guid id, [FromRoute] Guid shockerId)
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
    [ProducesResponseType((int) HttpStatusCode.OK)]
    [ProducesResponseType((int) HttpStatusCode.NotFound)]
    [ProducesResponseType((int) HttpStatusCode.BadRequest)]
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