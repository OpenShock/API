using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Services;
using OpenShock.Common.Authentication.Attributes;
using OpenShock.Common.Errors;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.Problems;

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
    [ProducesResponseType<LegacyEmptyResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ShockerNotFound
    [MapToApiVersion("1")]
    public async Task<IActionResult> RemoveShocker(
        [FromRoute] Guid shockerId,
        [FromServices] IDeviceUpdateService deviceUpdateService)
    {
        var affected = await _db.Shockers
            .Where(x => x.Id == shockerId)
            .WhereIsUserOrPrivileged(x => x.Device.Owner, CurrentUser)
            .FirstOrDefaultAsync();

        if (affected is null)
        {
            return Problem(ShockerError.ShockerNotFound);
        }

        _db.Shockers.Remove(affected);
        await _db.SaveChangesAsync();

        await deviceUpdateService.UpdateDeviceForAllShared(CurrentUser.Id, affected.DeviceId, DeviceUpdateType.ShockerUpdated);

        return LegacyEmptyOk("Shocker removed successfully");
    }
}