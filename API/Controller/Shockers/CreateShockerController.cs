using System.Net;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Requests;
using OpenShock.API.Realtime;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis.PubSub;
using OpenShock.ServicesCommon.Hubs;
using OpenShock.ServicesCommon.Services.Device;

namespace OpenShock.API.Controller.Shockers;

public sealed partial class ShockerController
{
    /// <summary>
    /// Creates a new Shocker.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="deviceService"></param>
    /// <param name="userHubContext"></param>
    /// <response code="201">Successfully created shocker</response>
    /// <response code="400">You can have a maximum of 11 Shockers per Device.</response>
    /// <response code="404">Device does not exist</response>
    [HttpPost(Name = "CreateShocker")]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [MapToApiVersion("1")]
    public async Task<BaseResponse<Guid>> CreateShocker(
        [FromBody] NewShocker data,
        [FromServices] IDeviceService deviceService)
    {
        var device = await _db.Devices.Where(x => x.Owner == CurrentUser.DbUser.Id && x.Id == data.Device).Select(x => x.Id).SingleOrDefaultAsync();
        if(device == Guid.Empty) return EBaseResponse<Guid>("Device does not exist", HttpStatusCode.NotFound);
        var shockerCount = await _db.Shockers.CountAsync(x => x.Device == data.Device);

        if (shockerCount >= 11) return EBaseResponse<Guid>("You can have a maximum of 11 Shockers per Device.");
        
        var shocker = new Shocker
        {
            Id = Guid.NewGuid(),
            Name = data.Name,
            RfId = data.RfId,
            Device = data.Device,
            Model = data.Model
        };
        _db.Shockers.Add(shocker);
        await _db.SaveChangesAsync();

        await deviceService.UpdateDeviceForAllShared(CurrentUser.DbUser.Id, device, DeviceUpdateType.ShockerUpdated);

        Response.StatusCode = (int)HttpStatusCode.Created;
        return new BaseResponse<Guid>
        {
            Data = shocker.Id
        };
    }
}