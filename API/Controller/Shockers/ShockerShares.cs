using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Requests;
using OpenShock.API.Models.Response;
using OpenShock.API.Services;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using System.Net.Mime;
using OpenShock.Common.Authentication.Attributes;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;

namespace OpenShock.API.Controller.Shockers;

public sealed partial class ShockerController
{
    /// <summary>
    /// Get all user shares for a shocker
    /// </summary>
    /// <param name="shockerId">Id of the shocker</param>
    /// <response code="200">OK</response>
    /// <response code="404">The shocker does not exist or you do not have access to it.</response>
    [HttpGet("{shockerId}/shares")]
    [ProducesResponseType<LegacyDataResponse<IAsyncEnumerable<ShareInfo>>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ShockerNotFound    
    [MapToApiVersion("1")]
    public async Task<IActionResult> GetUserShares([FromRoute] Guid shockerId)
    {
        var owns = await _db.Shockers.AnyAsync(x => x.Device.OwnerId == CurrentUser.Id && x.Id == shockerId);
        if (!owns) return Problem(ShockerError.ShockerNotFound);

        var shares = _db.UserShares
            .Where(x => x.ShockerId == shockerId && x.Shocker.Device.OwnerId == CurrentUser.Id)
            .Select(x =>
                new ShareInfo
                {
                    Paused = x.IsPaused,
                    SharedWith = new BasicUserInfo
                    {
                        Name = x.SharedWithUser.Name,
                        Id = x.SharedWithUserId,
                        Image = x.SharedWithUser.GetImageUrl()
                    },
                    CreatedOn = x.CreatedAt,
                    Permissions = new ShockerPermissions
                    {
                        Vibrate = x.AllowVibrate,
                        Sound = x.AllowSound,
                        Shock = x.AllowShock,
                        Live = x.AllowLiveControl
                    },
                    Limits = new ShockerLimits
                    {
                        Intensity = x.MaxIntensity,
                        Duration = x.MaxDuration
                    }
                }
            )
            .AsAsyncEnumerable();

        return LegacyDataOk(shares);
    }

    /// <summary>
    /// List all share codes for a shocker
    /// </summary>
    /// <param name="shockerId"></param>
    /// <response code="200">OK</response>
    [HttpGet("{shockerId}/shareCodes")]
    [ProducesResponseType<LegacyDataResponse<IAsyncEnumerable<ShareCodeInfo>>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ShockerNotFound    
    [MapToApiVersion("1")]
    public async Task<IActionResult> ShockerShareCodeList([FromRoute] Guid shockerId)
    {
        var owns = await _db.Shockers.AnyAsync(x => x.Device.OwnerId == CurrentUser.Id && x.Id == shockerId);
        if (!owns) return Problem(ShockerError.ShockerNotFound);

        var shares = _db.ShockerShareCodes
            .Where(x => x.ShockerId == shockerId && x.Shocker.Device.OwnerId == CurrentUser.Id)
            .Select(x => new ShareCodeInfo
            {
                CreatedOn = x.CreatedAt,
                Id = x.Id
            })
            .AsAsyncEnumerable();

        return LegacyDataOk(shares);
    }

    public sealed class ShareCodeInfo
    {
        public required Guid Id { get; set; }
        public required DateTime CreatedOn { get; set; }
    }

    /// <summary>
    /// Create a share code for a shocker
    /// </summary>
    /// <param name="shockerId"></param>
    /// <param name="body"></param>
    /// <param name="deviceUpdateService"></param>
    /// <response code="200">The share code was successfully created.</response>
    /// <response code="404">Shocker does not exists or you do not have access to it.</response>
    /// <returns></returns>
    [HttpPost("{shockerId}/shares")]
    [TokenPermission(PermissionType.Shockers_Edit)]
    [ProducesResponseType<LegacyDataResponse<Guid>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ShockerNotFound    
    [MapToApiVersion("1")]
    public async Task<IActionResult> ShockerShareCodeCreate(
        [FromRoute] Guid shockerId,
        [FromBody] ShockerPermLimitPair body,
        [FromServices] IDeviceUpdateService deviceUpdateService
    )
    {
        var device = await _db.Shockers.Where(x => x.Device.OwnerId == CurrentUser.Id && x.Id == shockerId)
            .Select(x => x.DeviceId).FirstOrDefaultAsync();
        if (device == Guid.Empty) return Problem(ShockerError.ShockerNotFound);

        var newCode = new ShockerShareCode
        {
            Id = Guid.CreateVersion7(),
            ShockerId = shockerId,
            AllowShock = body.Permissions.Shock,
            AllowVibrate = body.Permissions.Vibrate,
            AllowSound = body.Permissions.Sound,
            AllowLiveControl = body.Permissions.Live,
            MaxIntensity = body.Limits.Intensity,
            MaxDuration = body.Limits.Duration,
            IsPaused = false
        };
        _db.ShockerShareCodes.Add(newCode);
        await _db.SaveChangesAsync();

        await deviceUpdateService.UpdateDeviceForAllShared(CurrentUser.Id, device, DeviceUpdateType.ShockerUpdated);

        return LegacyDataOk(newCode.Id);
    }

    /// <summary>
    /// Remove a share code for a shocker
    /// </summary>
    /// <param name="shockerId"></param>
    /// <param name="sharedWithUserId"></param>
    /// <param name="deviceUpdateService"></param>
    /// <response code="200">Successfully removed share code</response>
    /// <response code="404">Share does not exists or device/shocker does not belong to you nor is shared with you</response>
    [HttpDelete("{shockerId}/shares/{sharedWithUserId}")]
    [TokenPermission(PermissionType.Shockers_Edit)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ShockerNotFound    
    [MapToApiVersion("1")]
    public async Task<IActionResult> ShockerShareCodeRemove(
        [FromRoute] Guid shockerId,
        [FromRoute] Guid sharedWithUserId,
        [FromServices] IDeviceUpdateService deviceUpdateService)
    {
        var affected = await _db.UserShares.Where(x =>
                x.ShockerId == shockerId && x.SharedWithUserId == sharedWithUserId &&
                (x.Shocker.Device.OwnerId == CurrentUser.Id || x.SharedWithUserId == CurrentUser.Id))
            .ExecuteDeleteAsync();
        if (affected <= 0) return Problem(ShockerError.ShockerNotFound);

        var device = await _db.Shockers.Where(x => x.Id == shockerId)
            .Select(x => new { x.DeviceId, x.Device.OwnerId }).SingleAsync();

        await deviceUpdateService.UpdateDevice(device.OwnerId, device.DeviceId, DeviceUpdateType.ShockerUpdated, sharedWithUserId);

        return Ok();
    }

    /// <summary>
    /// Update a share for a shocker
    /// </summary>
    /// <param name="shockerId"></param>
    /// <param name="sharedWithUserId"></param>
    /// <param name="body"></param>
    /// <param name="deviceUpdateService"></param>
    /// <response code="200">Successfully updated share code</response>
    /// <response code="404">The share code does not exist or you do not have access to it.</response>
    [HttpPatch("{shockerId}/shares/{sharedWithUserId}")]
    [TokenPermission(PermissionType.Shockers_Edit)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ShockerNotFound
    [MapToApiVersion("1")]
    public async Task<IActionResult> ShockerShareCodeUpdate(
        [FromRoute] Guid shockerId,
        [FromRoute] Guid sharedWithUserId,
        [FromBody] ShockerPermLimitPair body,
        [FromServices] IDeviceUpdateService deviceUpdateService)
    {
        var affected = await _db.UserShares.Where(x =>
                x.ShockerId == shockerId && x.SharedWithUserId == sharedWithUserId &&
                x.Shocker.Device.OwnerId == CurrentUser.Id).Select(x =>
                new { Share = x, x.Shocker.DeviceId, Owner = x.Shocker.Device.OwnerId })
            .FirstOrDefaultAsync();
        if (affected == null) return Problem(ShockerError.ShockerNotFound);

        var share = affected.Share;
        
        share.AllowShock = body.Permissions.Shock;
        share.AllowVibrate = body.Permissions.Vibrate;
        share.AllowSound = body.Permissions.Sound;
        share.AllowLiveControl = body.Permissions.Live;
        share.MaxIntensity = body.Limits.Intensity;
        share.MaxDuration = body.Limits.Duration;

        await _db.SaveChangesAsync();

        await deviceUpdateService.UpdateDevice(affected.Owner, affected.DeviceId, DeviceUpdateType.ShockerUpdated, sharedWithUserId);

        return Ok();
    }

    /// <summary>
    /// Pause/Unpause a share code for a shocker
    /// </summary>
    /// <param name="shockerId"></param>
    /// <param name="sharedWithUserId"></param>
    /// <param name="body"></param>
    /// <param name="deviceUpdateService"></param>
    /// <response code="200">Successfully updated pause status share</response>
    /// <response code="404">The share code does not exist or you do not have access to it.</response>
    [HttpPost("{shockerId}/shares/{sharedWithUserId}/pause")]
    [TokenPermission(PermissionType.Shockers_Pause)]
    [ProducesResponseType<LegacyDataResponse<bool>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ShockerNotFound    
    [MapToApiVersion("1")]
    public async Task<IActionResult> ShockerShareCodePause(
        [FromRoute] Guid shockerId,
        [FromRoute] Guid sharedWithUserId,
        [FromBody] PauseRequest body,
        [FromServices] IDeviceUpdateService deviceUpdateService)
    {
        var affected = await _db.UserShares.Where(x =>
            x.ShockerId == shockerId && x.SharedWithUserId == sharedWithUserId &&
            x.Shocker.Device.OwnerId == CurrentUser.Id).Select(x =>
            new { Share = x, x.Shocker.DeviceId, Owner = x.Shocker.Device.OwnerId })
            .FirstOrDefaultAsync();
        if (affected == null) return Problem(ShockerError.ShockerNotFound);

        affected.Share.IsPaused = body.Pause;

        await _db.SaveChangesAsync();

        await deviceUpdateService.UpdateDevice(affected.Owner, affected.DeviceId, DeviceUpdateType.ShockerUpdated, sharedWithUserId);

        return LegacyDataOk(body.Pause);
    }
}