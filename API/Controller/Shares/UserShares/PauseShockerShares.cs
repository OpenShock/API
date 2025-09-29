using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OpenShock.API.Services.DeviceUpdate;
using OpenShock.API.Utils;
using OpenShock.Common.DeviceControl;
using OpenShock.Common.Errors;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.Services;

namespace OpenShock.API.Controller.Shares.UserShares;

public sealed partial class UserSharesController
{
    [HttpPost("{userId:guid}/shockers/pause")]
    [MapToApiVersion("2")]
    [ProducesResponseType<PauseUserShareShockersResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkPauseUserShareShockers(
        [FromRoute] Guid userId,
        [FromBody] PauseUserShareShockersRequest pauseRequest,
        [FromServices] IDeviceUpdateService deviceUpdateService)
    {
        var affected = await _db.UserShares
            .Include(x => x.Shocker)
            .Where(x => pauseRequest.Shockers.Contains(x.ShockerId) && x.SharedWithUserId == userId &&
                        x.Shocker.Device.OwnerId == CurrentUser.Id)
            .ToArrayAsync();

        // Check if we have all shockers to delete
        var missingShockers = pauseRequest.Shockers.Except(affected.Select(x => x.ShockerId)).ToArray();
        if (missingShockers.Length > 0)
        {
            return Problem(ShareError.UserShareNotFound);
        }

        foreach (var userShare in affected)
        {
            userShare.IsPaused = pauseRequest.Pause;
            _db.UserShares.Update(userShare);
        }

        var deletedRecords = await _db.SaveChangesAsync();

        var uniqueHubIds = affected.Select(x => x.Shocker.DeviceId).Distinct();
        var updateTasks = uniqueHubIds.Select(device =>
            deviceUpdateService.UpdateDevice(CurrentUser.Id, device, DeviceUpdateType.ShockerUpdated, userId));
        await Task.WhenAll(updateTasks);

        return Ok(new PauseUserShareShockersResponse()
        {
            AffectedRecords = deletedRecords,
            PauseStates = affected.ToDictionary(x => x.ShockerId, x => UserShareUtils.GetPausedReason(x.IsPaused, x.Shocker.IsPaused))
        });
    }
}

public sealed class PauseUserShareShockersRequest
{
    [Required, MinLength(1)] public required Guid[] Shockers { get; set; }

    [Required] public required bool Pause { get; set; }
}

public sealed class PauseUserShareShockersResponse
{
    public required int AffectedRecords { get; set; }
    public required IReadOnlyDictionary<Guid, PauseReason> PauseStates { get; set; }
}