using System.Net;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Requests;
using OpenShock.API.Services;
using OpenShock.Common.Models;
using OpenShock.ServicesCommon.Authentication.Attributes;
using OpenShock.ServicesCommon.Errors;
using OpenShock.ServicesCommon.Problems;

namespace OpenShock.API.Controller.Shockers;

public sealed partial class ShockerController
{
    /// <summary>
    /// Pause or unpause a shocker
    /// </summary>
    /// <param name="shockerId"></param>
    /// <param name="body"></param>
    /// <param name="deviceUpdateService"></param>
    /// <response code="200">Successfully set pause state</response>
    /// <response code="404">Shocker not found or does not belong to you</response>
    [HttpPost("{shockerId}/pause")]
    [TokenPermission(PermissionType.Shockers_Pause)]
    [ProducesSuccess<bool?>]
    [ProducesProblem(HttpStatusCode.NotFound, "ShockerNotFound")]    
    [MapToApiVersion("1")]
    public async Task<IActionResult> PauseShocker([FromRoute] Guid shockerId, [FromBody] PauseRequest body,
        [FromServices] IDeviceUpdateService deviceUpdateService)
    {
        var shocker = await _db.Shockers.Where(x => x.Id == shockerId && x.DeviceNavigation.Owner == CurrentUser.DbUser.Id)
            .SingleOrDefaultAsync();
        if (shocker == null) return Problem(ShockerError.ShockerNotFound);
        shocker.Paused = body.Pause;
        await _db.SaveChangesAsync();

        await deviceUpdateService.UpdateDeviceForAllShared(CurrentUser.DbUser.Id, shocker.Device, DeviceUpdateType.ShockerUpdated);

        return RespondSuccess(body.Pause);
    }
}