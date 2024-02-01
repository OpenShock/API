using System.Net;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Requests;
using OpenShock.API.Realtime;
using OpenShock.API.Services;
using OpenShock.Common.Models;
using OpenShock.Common.Redis.PubSub;
using OpenShock.ServicesCommon.Services.Device;

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
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [MapToApiVersion("1")]
    public async Task<BaseResponse<bool?>> PauseShocker([FromRoute] Guid shockerId, [FromBody] PauseRequest body,
        [FromServices] IDeviceUpdateService deviceUpdateService)
    {
        var shocker = await _db.Shockers.Where(x => x.Id == shockerId && x.DeviceNavigation.Owner == CurrentUser.DbUser.Id)
            .SingleOrDefaultAsync();
        if (shocker == null)
            return EBaseResponse<bool?>("Shocker not found or does not belong to you", HttpStatusCode.NotFound);
        shocker.Paused = body.Pause;
        await _db.SaveChangesAsync();

        await deviceUpdateService.UpdateDeviceForAllShared(CurrentUser.DbUser.Id, shocker.Device, DeviceUpdateType.ShockerUpdated);

        return new BaseResponse<bool?>("Successfully set pause state", body.Pause);
    }
}