using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Realtime;
using OpenShock.Common.Models;
using OpenShock.Common.Redis.PubSub;

namespace OpenShock.API.Controller.Shockers;

public sealed partial class ShockerController
{
    [HttpDelete("{id:guid}")]
    public async Task<BaseResponse<object>> DeleteShocker(Guid id)
    {
        var affected = await _db.Shockers.Where(x => x.DeviceNavigation.Owner == CurrentUser.DbUser.Id && x.Id == id).SingleOrDefaultAsync();
        
        if (affected == null)
            return EBaseResponse<object>("Shocker does not exist", HttpStatusCode.NotFound);
        
        _db.Shockers.Remove(affected);
        await _db.SaveChangesAsync();
        
        await PubSubManager.SendDeviceUpdate(new DeviceUpdatedMessage
        {
            Id = affected.Device
        });
        
        return new BaseResponse<object>
        {
            Message = "Successfully deleted shocker"
        };
    }
}