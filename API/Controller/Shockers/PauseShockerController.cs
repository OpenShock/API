using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShockLink.API.Authentication;
using ShockLink.API.Models;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller.Shockers;

[ApiController]
[Route("/{version:apiVersion}/shockers")]
public class PauseShockersController : AuthenticatedSessionControllerBase
{
    private readonly ShockLinkContext _db;

    public PauseShockersController(ShockLinkContext db)
    {
        _db = db;
    }

    [HttpPost("{id:guid}/pause")]
    public async Task<BaseResponse<object>> Pause(Guid id, bool pause)
    {
        var shocker = await _db.Shockers.Where(x => x.Id == id && x.DeviceNavigation.Owner == CurrentUser.DbUser.Id)
            .SingleOrDefaultAsync();
        if (shocker == null)
            return EBaseResponse<object>("Shocker not found or does not belong to you", HttpStatusCode.NotFound);
        shocker.Paused = pause;
        await _db.SaveChangesAsync();
        return new BaseResponse<object>("Successfully set pause state");
    }
}