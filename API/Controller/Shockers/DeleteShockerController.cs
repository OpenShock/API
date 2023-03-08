using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShockLink.API.Authentication;
using ShockLink.API.Models;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller.Shockers;

[ApiController]
[Route("/{version:apiVersion}/shockers")]
public class DeleteShockerController : AuthenticatedSessionControllerBase
{
    private readonly ShockLinkContext _db;
    
    public DeleteShockerController(ShockLinkContext db)
    {
        _db = db;
    }
    
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