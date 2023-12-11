using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Requests;
using OpenShock.API.Realtime;
using OpenShock.Common.Models;
using OpenShock.Common.Redis.PubSub;

namespace OpenShock.API.Controller.Shockers;

public sealed partial class ShockerController
{
    /// <summary>
    /// Edits a shocker
    /// </summary>
    /// <param name="id"></param>
    /// <param name="data"></param>
    /// <response code="200">Successfully updated shocker</response>
    /// <response code="404">Shocker does not exist</response>
    [HttpPatch("{id}", Name = "EditShocker")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<BaseResponse<object>> EditShocker([FromRoute] Guid id, [FromBody] NewShocker data)
    {
        var device = await _db.Devices.AnyAsync(x => x.Owner == CurrentUser.DbUser.Id && x.Id == data.Device);
        if (!device)
            return EBaseResponse<object>("Device does not exists or you do not have access to it",
                HttpStatusCode.NotFound);

        var shocker = await _db.Shockers.Where(x => x.DeviceNavigation.Owner == CurrentUser.DbUser.Id && x.Id == id)
            .SingleOrDefaultAsync();
        if (shocker == null) return EBaseResponse<object>("Shocker does not exist", HttpStatusCode.NotFound);
        var oldDevice = shocker.Device;

        shocker.Device = data.Device;
        shocker.Name = data.Name;
        shocker.RfId = data.RfId;
        shocker.Model = data.Model;

        await _db.SaveChangesAsync();

        await Task.WhenAll(
            PubSubManager.SendDeviceUpdate(new DeviceUpdatedMessage
            {
                Id = oldDevice
            }),
            PubSubManager.SendDeviceUpdate(new DeviceUpdatedMessage
            {
                Id = data.Device
            })
        );

        return new BaseResponse<object>
        {
            Message = "Successfully updated shocker"
        };
    }
}