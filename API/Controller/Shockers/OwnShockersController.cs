using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShockLink.API.Authentication;
using ShockLink.API.Models;
using ShockLink.API.Models.Response;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller.Shockers;

[ApiController]
[Route("/{version:apiVersion}/shockers/own")]
public class OwnShockersController : AuthenticatedSessionControllerBase
{
    private readonly ShockLinkContext _db;
    
    public OwnShockersController(ShockLinkContext db)
    {
        _db = db;
    }
    
    [HttpGet]
    public async Task<BaseResponse<IEnumerable<DeviceWithShockers>>> GetOwnShockers()
    {
        var shockers = await _db.Devices.Where(x => x.Owner == CurrentUser.DbUser.Id).Select(x => new DeviceWithShockers
        {
            Id = x.Id,
            Name = x.Name,
            CreatedOn = x.CreatedOn,
            Shockers = x.Shockers.Select(y => new ShockerResponse
            {
                Id = y.Id,
                Name = y.Name,
                RfId = y.RfId,
                CreatedOn = y.CreatedOn
            })
        }).ToListAsync();
        
        return new BaseResponse<IEnumerable<DeviceWithShockers>>
        {
            Data = shockers
        };
    }
}