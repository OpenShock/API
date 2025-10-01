using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Services.DeviceUpdate;
using OpenShock.Common.Errors;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;

namespace OpenShock.API.Controller.Shares.UserShares;

public sealed partial class UserSharesController
{
    [HttpDelete("{userId:guid}/shockers")]
    [MapToApiVersion("2")]
    [ProducesResponseType<RemoveUserSharesResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkRemoveUserShareShockers([FromRoute] Guid userId , [FromBody] [MinLength(1)] Guid[] shockerIds, [FromServices] IDeviceUpdateService deviceUpdateService)
    {
        var affected = await _db.UserShares
            .Where(x => shockerIds.Contains(x.ShockerId) && x.SharedWithUserId == userId &&
                        (x.Shocker.Device.OwnerId == CurrentUser.Id || x.SharedWithUserId == CurrentUser.Id))
            .Select(x => new { DeviceId = x.Shocker.Device.Id, OwnerId = x.Shocker.Device.OwnerId, UserShare = x })
            .ToArrayAsync();
        
        // Check if we have all shockers to delete
        var missingShockers = shockerIds.Except(affected.Select(x => x.UserShare.ShockerId)).ToArray();
        if (missingShockers.Length > 0)
        {
            return Problem(ShareError.UserShareNotFound);
        }
        
        _db.UserShares.RemoveRange(affected.Select(x => x.UserShare));

        var deletedRecords = await _db.SaveChangesAsync();

        var updateTasks = affected.DistinctBy(x => x.DeviceId).Select(x =>
            deviceUpdateService.UpdateDevice(x.OwnerId, x.DeviceId, DeviceUpdateType.ShockerUpdated, userId));
        await Task.WhenAll(updateTasks);
        
        return Ok(new RemoveUserSharesResponse { AffectedRecords = deletedRecords });
    }
}

public sealed class RemoveUserSharesResponse
{
    public required int AffectedRecords { get; set; }
}