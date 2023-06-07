using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShockLink.API.Authentication;
using ShockLink.API.Models;
using ShockLink.API.Models.Response;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller.Shockers;

[ApiController]
[Route("/{version:apiVersion}/shockers")]
public class GetShockerController : AuthenticatedSessionControllerBase
{
    private readonly ShockLinkContext _db;
    
    public GetShockerController(ShockLinkContext db)
    {
        _db = db;
    }
    
    [HttpGet("{id:guid}")]
    public async Task<BaseResponse<ShockerWithDevice>> GetShocker(Guid id)
    {
        var shocker = await _db.Shockers.Where(x => x.DeviceNavigation.Owner == CurrentUser.DbUser.Id && x.Id == id).Select(x => new ShockerWithDevice
        {
            Id = x.Id,
            Name = x.Name,
            RfId = x.RfId,
            CreatedOn = x.CreatedOn,
            Device = x.Device,
            Model = x.Model,
            IsPaused = x.Paused
        }).SingleOrDefaultAsync();

        if (shocker == null)
            return EBaseResponse<ShockerWithDevice>("Shocker does not exist", HttpStatusCode.NotFound);
        return new BaseResponse<ShockerWithDevice>
        {
            Data = shocker
        };
    }
}