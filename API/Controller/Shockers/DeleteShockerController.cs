using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShockLink.API.Models;

namespace ShockLink.API.Controller.Shockers;

public sealed partial class ShockerController
{
    [HttpDelete("{id:guid}")]
    public async Task<BaseResponse<object>> DeleteShocker(Guid id)
    {
        var affected = await _db.Shockers.Where(x => x.DeviceNavigation.Owner == CurrentUser.DbUser.Id && x.Id == id).ExecuteDeleteAsync();
        
        if (affected <= 0)
            return EBaseResponse<object>("Shocker does not exist", HttpStatusCode.NotFound);
        return new BaseResponse<object>
        {
            Message = "Successfully deleted shocker"
        };
    }
}