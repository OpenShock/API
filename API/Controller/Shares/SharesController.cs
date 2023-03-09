using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShockLink.API.Authentication;
using ShockLink.API.Models;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller.Shares;

[ApiController]
[Route("/{version:apiVersion}/shares")]
public class SharesController : AuthenticatedSessionControllerBase
{
    private readonly ShockLinkContext _db;

    public SharesController(ShockLinkContext db)
    {
        _db = db;
    }
    
    [HttpDelete("{id:guid}")]
    public async Task<BaseResponse<object>> DeleteCode(Guid id) {
        //var affected = await _db.ShockerShareCodes.Where(x => x.Id == id && x.)
        var device = await _db.Shockers.Where(x => x.DeviceNavigation.Owner == CurrentUser.DbUser.Id && x.Id == id).AnyAsync();
        if (!device)
            return EBaseResponse<object>("Device/Shocker does not exists or device does not belong to you",
                HttpStatusCode.NotFound);

        return null;
    }

}