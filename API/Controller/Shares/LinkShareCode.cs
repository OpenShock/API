using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Services;
using OpenShock.Common.Errors;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;

namespace OpenShock.API.Controller.Shares;

public sealed partial class SharesController
{
    /// <summary>
    /// Link a share code to your account
    /// </summary>
    /// <param name="shareCodeId"></param>
    /// <param name="deviceUpdateService"></param>
    /// <response code="200">Linked share code</response>
    /// <response code="404">Share code not found or does not belong to you</response>
    /// <response code="400">You cannot link your own shocker code / You already have this shocker linked to your account</response>
    /// <response code="500">Error while linking share code to your account</response>
    [HttpPost("code/{shareCodeId}")]
    [ProducesResponseType<LegacyEmptyResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ShareCodeNotFound
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)] // CantLinkOwnShareCode, ShockerAlreadyLinked
    [MapToApiVersion("1")]
    public async Task<IActionResult> LinkShareCode(
        [FromRoute] Guid shareCodeId,
        [FromServices] IDeviceUpdateService deviceUpdateService
    )
    {
        var shareCode = await _db.ShockerShareCodes.Where(x => x.Id == shareCodeId).Select(x => new
        {
            Share = x, x.Shocker.Device.OwnerId, x.Shocker.DeviceId
        }).FirstOrDefaultAsync();
        if (shareCode == null) return Problem(ShareCodeError.ShareCodeNotFound);
        if (shareCode.OwnerId == CurrentUser.Id) return Problem(ShareCodeError.CantLinkOwnShareCode);
        if (await _db.ShockerShares.AnyAsync(x => x.ShockerId == shareCode.Share.ShockerId && x.SharedWithUserId == CurrentUser.Id))
            return Problem(ShareCodeError.ShockerAlreadyLinked);
        
        _db.ShockerShares.Add(new UserShare
        {
            OwnerId = shareCode.OwnerId,
            SharedWithUserId = CurrentUser.Id,
            ShockerId = shareCode.Share.ShockerId,
            AllowShock = shareCode.Share.AllowShock,
            AllowVibrate = shareCode.Share.AllowVibrate,
            AllowSound = shareCode.Share.AllowSound,
            AllowLiveControl = shareCode.Share.AllowLiveControl,
            MaxIntensity = shareCode.Share.MaxIntensity,
            MaxDuration = shareCode.Share.MaxDuration,
            IsPaused = shareCode.Share.IsPaused
        });
        _db.ShockerShareCodes.Remove(shareCode.Share);

        if (await _db.SaveChangesAsync() <= 1) throw new Exception("Error while linking share code to your account");

        await deviceUpdateService.UpdateDevice(shareCode.OwnerId, shareCode.DeviceId, DeviceUpdateType.ShockerUpdated, CurrentUser.Id);

        return LegacyEmptyOk("Successfully linked share code");
    }
}