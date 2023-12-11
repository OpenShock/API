using System.Net;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Requests;
using OpenShock.API.Models.Response;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.ServicesCommon.Utils;

namespace OpenShock.API.Controller.Shockers;

public sealed partial class ShockerController
{
    /// <summary>
    /// Gets information about the authenticated device.
    /// </summary>
    /// <param name="id"></param>
    /// <response code="200">The device information was successfully retrieved.</response>
    /// <response code="404">Device does not exists or you do not have access to it.</response>
    [HttpGet("{id}/shares", Name = "GetShares")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [MapToApiVersion("1")]
    public async Task<BaseResponse<IEnumerable<ShareInfo>>> GetShares([FromRoute] Guid id)
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
    /// Gets share codes for a shocker
    /// </summary>
    /// <param name="id"></param>
    /// <response code="200">The device information was successfully retrieved.</response>
    /// <response code="404">Device does not exists or you do not have access to it.</response>
    [HttpGet("{id}/shareCodes", Name = "GetShareCodes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [MapToApiVersion("1")]
    public async Task<BaseResponse<IEnumerable<ShareCodeInfo>>> GetShareCodes([FromRoute] Guid id)
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
    /// Creates a share code for a shocker
    /// </summary>
    /// <param name="id"></param>
    /// <param name="data"></param>
    /// <response code="200">The device information was successfully retrieved.</response>
    /// <response code="404">Device does not exists or you do not have access to it.</response>
    /// <returns></returns>
    [HttpPost("{id}/shares", Name = "CreateShare")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [MapToApiVersion("1")]
    public async Task<BaseResponse<Guid>> CreateShareCode([FromRoute] Guid id, [FromBody] CreateShareCode data)
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
    
    /// <summary>
    /// Deletes a share code for a shocker
    /// </summary>
    /// <param name="id"></param>
    /// <param name="sharedWith"></param>
    /// <response code="200">The device information was successfully retrieved.</response>
    /// <response code="404">Device does not exists or you do not have access to it.</response>
    [HttpDelete("{id}/shares/{sharedWith}", Name = "DeleteShare")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [MapToApiVersion("1")]
    public async Task<BaseResponse<object>> DeleteShare([FromRoute] Guid id, [FromRoute] Guid sharedWith)
    {
        var affected = await _db.ShockerShares.Where(x =>
            x.ShockerId == id && x.SharedWith == sharedWith && (x.Shocker.DeviceNavigation.Owner == CurrentUser.DbUser.Id || x.SharedWith == CurrentUser.DbUser.Id)).ExecuteDeleteAsync();
        if (affected <= 0)
            return EBaseResponse<object>("Share does not exists or device/shocker does not belong to you nor is shared with you",
                HttpStatusCode.NotFound);

        return new BaseResponse<object>("Successfully deleted share");
    }
    
    /// <summary>
    /// Updates a share code for a shocker
    /// </summary>
    /// <param name="id"></param>
    /// <param name="sharedWith"></param>
    /// <param name="data"></param>
    /// <response code="200">The device information was successfully retrieved.</response>
    /// <response code="404">Device does not exists or you do not have access to it.</response>
    [HttpPatch("{id}/shares/{sharedWith}", Name = "UpdateShare")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [MapToApiVersion("1")]
    public async Task<BaseResponse<object>> UpdateShare([FromRoute] Guid id, [FromRoute] Guid sharedWith, [FromBody] CreateShareCode data)
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
    
    /// <summary>
    /// Pauses a share code for a shocker
    /// </summary>
    /// <param name="id"></param>
    /// <param name="sharedWith"></param>
    /// <param name="data"></param>
    /// <response code="200">The device information was successfully retrieved.</response>
    /// <response code="404">Device does not exists or you do not have access to it.</response>
    [HttpPost("{id}/shares/{sharedWith}/pause", Name = "PauseShare")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [MapToApiVersion("1")]
    public async Task<BaseResponse<object>> UpdatePauseShare([FromRoute] Guid id, [FromRoute] Guid sharedWith, [FromBody] PauseRequest data)
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