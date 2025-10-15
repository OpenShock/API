using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Requests;
using OpenShock.API.Services.DeviceUpdate;
using OpenShock.Common.Authentication.Attributes;
using OpenShock.Common.Errors;
using OpenShock.Common.Models;
using OpenShock.Common.Problems;

namespace OpenShock.API.Controller.Shares.UserShares;

public sealed partial class UserSharesController
{
    /// <summary>
    /// Update user shares for a shocker
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="body"></param>
    /// <param name="deviceUpdateService"></param>
    /// <response code="200">Successfully updated share code</response>
    /// <response code="404">The share code does not exist or you do not have access to it.</response>
    [HttpPatch("{userId:guid}/shockers")]
    [TokenPermission(PermissionType.Shockers_Edit)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound,
        MediaTypeNames.Application.ProblemJson)] // ShockerNotFound
    [MapToApiVersion("2")]
    public async Task<IActionResult> BulkUserShareShockersUpdate(
        [FromRoute] Guid userId,
        [FromBody] BulkUserShareShockersUpdateRequest body,
        [FromServices] IDeviceUpdateService deviceUpdateService)
    {
        var affected = await _db.UserShares.Where(x =>
                body.Shockers.Contains(x.ShockerId) && x.SharedWithUserId == userId &&
                x.Shocker.Device.OwnerId == CurrentUser.Id).Select(x =>
                new { Share = x, x.Shocker.DeviceId, Owner = x.Shocker.Device.OwnerId })
            .ToArrayAsync();
        
        var missingShockers = body.Shockers.Except(affected.Select(x => x.Share.ShockerId)).ToArray();
        if (missingShockers.Length > 0)
        {
            return Problem(ShockerError.ShockerNotFound);
        }
        
        foreach (var share in affected)
        {
            share.Share.AllowShock = body.Permissions.Shock;
            share.Share.AllowVibrate = body.Permissions.Vibrate;
            share.Share.AllowSound = body.Permissions.Sound;
            share.Share.AllowLiveControl = body.Permissions.Live;
            share.Share.MaxIntensity = body.Limits.Intensity;
            share.Share.MaxDuration = body.Limits.Duration;

            _db.UserShares.Update(share.Share);
        }

        await _db.SaveChangesAsync();

        var uniqueHubIds = affected.Select(x => x.Share.Shocker.DeviceId).Distinct();
        var updateTasks = uniqueHubIds.Select(device =>
            deviceUpdateService.UpdateDevice(CurrentUser.Id, device, DeviceUpdateType.ShockerUpdated, userId));
        await Task.WhenAll(updateTasks);

        return Ok();
    }
}

public sealed class BulkUserShareShockersUpdateRequest : ShockerPermLimitPair
{
    public required IReadOnlyList<Guid> Shockers { get; set; }
}