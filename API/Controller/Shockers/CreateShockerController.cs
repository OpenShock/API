using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Requests;
using OpenShock.API.Realtime;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis.PubSub;

namespace OpenShock.API.Controller.Shockers;

public sealed partial class ShockerController
{
    [HttpPost]
    public async Task<BaseResponse<Guid>> CreateShocker(NewShocker data)
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
        
        await PubSubManager.SendDeviceUpdate(new DeviceUpdatedMessage
        {
            Id = device
        });

        Response.StatusCode = (int)HttpStatusCode.Created;
        return new BaseResponse<Guid>
        {
            Data = shocker.Id
        };
    }
}