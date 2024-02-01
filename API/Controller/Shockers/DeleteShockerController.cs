using System.Net;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Services;
using OpenShock.Common.Models;
using OpenShock.ServicesCommon.Services.Device;

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
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [MapToApiVersion("1")]
    public async Task<BaseResponse<object>> RemoveShocker(
        [FromRoute] Guid shockerId,
        [FromServices] IDeviceUpdateService deviceUpdateService)
    {
        var affected = await _db.Shockers.Where(x => x.DeviceNavigation.Owner == CurrentUser.DbUser.Id && x.Id == shockerId)
            .SingleOrDefaultAsync();

        if (affected == null)
            return EBaseResponse<object>("Shocker does not exist", HttpStatusCode.NotFound);

        _db.Shockers.Remove(affected);
        await _db.SaveChangesAsync();

        await deviceUpdateService.UpdateDeviceForAllShared(CurrentUser.DbUser.Id, affected.Device, DeviceUpdateType.ShockerUpdated);

        return new BaseResponse<object>
        {
            Message = "Successfully deleted shocker"
        };
    }
}