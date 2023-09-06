using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShockLink.API.Models;
using ShockLink.API.Models.Requests;

namespace ShockLink.API.Controller.Shockers;

public sealed partial class ShockerController
{
    [HttpPost("{id:guid}/pause")]
    public async Task<BaseResponse<bool?>> Pause(Guid id, PauseRequest data)
    {
        var shocker = await _db.Shockers.Where(x => x.Id == id && x.DeviceNavigation.Owner == CurrentUser.DbUser.Id)
            .SingleOrDefaultAsync();
        if (shocker == null)
            return EBaseResponse<bool?>("Shocker not found or does not belong to you", HttpStatusCode.NotFound);
        shocker.Paused = data.Pause;
        await _db.SaveChangesAsync();
        return new BaseResponse<bool?>("Successfully set pause state", data.Pause);
    }
    

}