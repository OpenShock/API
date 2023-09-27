using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShockLink.API.Models;
using ShockLink.API.Models.Requests;
using ShockLink.Common.Models;

namespace ShockLink.API.Controller.Shockers;

public sealed partial class ShockerController
{
    [HttpPatch("{id:guid}")]
    public async Task<BaseResponse<object>> EditShocker(Guid id, NewShocker data)
    {
        var device = await _db.Devices.AnyAsync(x => x.Owner == CurrentUser.DbUser.Id && x.Id == data.Device);
        if(!device) return EBaseResponse<object>("Device does not exists or you do not have access to it", HttpStatusCode.NotFound);
        var shocker = await _db.Shockers.Where(x => x.DeviceNavigation.Owner == CurrentUser.DbUser.Id && x.Id == id).SingleOrDefaultAsync();
        if (shocker == null) return EBaseResponse<object>("Shocker does not exist", HttpStatusCode.NotFound);

        shocker.Device = data.Device;
        shocker.Name = data.Name;
        shocker.RfId = data.RfId;
        shocker.Model = data.Model;

        await _db.SaveChangesAsync();
        
        return new BaseResponse<object>
        {
            Message = "Successfully updated shocker"
        };
    }
}