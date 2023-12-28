using System.Net;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Realtime;
using OpenShock.Common.Models;
using OpenShock.Common.Redis.PubSub;
using OpenShock.ServicesCommon.Hubs;
using OpenShock.ServicesCommon.Services.Device;

namespace OpenShock.API.Controller.Shockers;

public sealed partial class ShockerController
{
    /// <summary>
    /// Deletes a shocker
    /// </summary>
    /// <param name="id"></param>
    /// <param name="deviceService"></param>
    /// <param name="userHubContext"></param>
    /// <response code="200">Successfully deleted shocker</response>
    /// <response code="404">Shocker does not exist</response>
    [HttpDelete("{id}", Name = "DeleteShocker")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [MapToApiVersion("1")]
    public async Task<BaseResponse<object>> DeleteShocker(
        [FromRoute] Guid id,
        [FromServices] IDeviceService deviceService)
    {
        var affected = await _db.Shockers.Where(x => x.DeviceNavigation.Owner == CurrentUser.DbUser.Id && x.Id == id)
            .SingleOrDefaultAsync();

        if (affected == null)
            return EBaseResponse<object>("Shocker does not exist", HttpStatusCode.NotFound);

        _db.Shockers.Remove(affected);
        await _db.SaveChangesAsync();

        await deviceService.UpdateDevice(CurrentUser.DbUser.Id, affected.Device, DeviceUpdateType.ShockerUpdated);

        return new BaseResponse<object>
        {
            Message = "Successfully deleted shocker"
        };
    }
}