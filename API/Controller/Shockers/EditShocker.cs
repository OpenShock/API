using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Requests;
using OpenShock.API.Services.DeviceUpdate;
using OpenShock.Common.Authentication.Attributes;
using OpenShock.Common.Errors;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;

using OpenShock.Internal.Common.Problems;

namespace OpenShock.API.Controller.Shockers;

public sealed partial class ShockerController
{
    /// <summary>
    /// Edit a shocker
    /// </summary>
    /// <param name="shockerId"></param>
    /// <param name="body"></param>
    /// <param name="deviceUpdateService"></param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">Successfully updated shocker</response>
    /// <response code="404">Shocker does not exist</response>
    [HttpPatch("{shockerId}")]
    [TokenPermission(PermissionType.Shockers_Edit)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<LegacyEmptyResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // DeviceNotFound, ShockerNotFound
    [MapToApiVersion("1")]
    public async Task<IActionResult> EditShocker(
        [FromRoute] Guid shockerId,
        [FromBody] NewShocker body, 
        [FromServices] IDeviceUpdateService deviceUpdateService,
        CancellationToken cancellationToken)
    {
        var device = await _db.Devices.AnyAsync(x => x.OwnerId == CurrentUser.Id && x.Id == body.Device, cancellationToken);
        if (!device) return Problem(HubError.HubNotFound);

        var shocker = await _db.Shockers.FirstOrDefaultAsync(x => x.Device.OwnerId == CurrentUser.Id && x.Id == shockerId, cancellationToken);
        if (shocker is null) return Problem(ShockerError.ShockerNotFound);
        var oldDevice = shocker.DeviceId;

        shocker.DeviceId = body.Device;
        shocker.Name = body.Name;
        shocker.RfId = body.RfId;
        shocker.Model = body.Model;

        await _db.SaveChangesAsync(cancellationToken);
        
        if (oldDevice != body.Device) 
            await deviceUpdateService.UpdateDeviceForAllShared(CurrentUser.Id, oldDevice, DeviceUpdateType.ShockerUpdated, cancellationToken);
        
        await deviceUpdateService.UpdateDeviceForAllShared(CurrentUser.Id, body.Device, DeviceUpdateType.ShockerUpdated, cancellationToken);
        
        return LegacyEmptyOk("Shocker updated successfully");
    }
}