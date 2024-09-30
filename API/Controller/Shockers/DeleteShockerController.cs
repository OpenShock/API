using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Services;
using OpenShock.Common.Models;
using OpenShock.ServicesCommon.Authentication.Attributes;
using OpenShock.ServicesCommon.Errors;
using OpenShock.ServicesCommon.Problems;
using System.Net;

namespace OpenShock.API.Controller.Shockers;

public sealed partial class ShockerController
{
    /// <summary>
    /// Remove a shocker
    /// </summary>
    /// <param name="shockerId"></param>
    /// <param name="deviceUpdateService"></param>
    /// <response code="200">Successfully deleted shocker</response>
    /// <response code="404">Shocker does not exist</response>
    [HttpDelete("{shockerId}")]
    [TokenPermission(PermissionType.Shockers_Edit)]
    [ProducesSuccess]
    [ProducesProblem(HttpStatusCode.NotFound, "ShockerNotFound")]
    [MapToApiVersion("1")]
    public async Task<IActionResult> RemoveShocker(
        [FromRoute] Guid shockerId,
        [FromServices] IDeviceUpdateService deviceUpdateService)
    {
        var affected = await _db.Shockers.Where(x => x.DeviceNavigation.Owner == CurrentUser.DbUser.Id && x.Id == shockerId)
            .SingleOrDefaultAsync();

        if (affected == null) return Problem(ShockerError.ShockerNotFound);

        _db.Shockers.Remove(affected);
        await _db.SaveChangesAsync();

        await deviceUpdateService.UpdateDeviceForAllShared(CurrentUser.DbUser.Id, affected.Device, DeviceUpdateType.ShockerUpdated);

        return RespondSuccessSimple("Shocker removed successfully");
    }
}