using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShockLink.API.Authentication;
using ShockLink.API.Models;
using ShockLink.API.Models.Response;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller.Devices;

[ApiController]
[Route("/{version:apiVersion}/devices")]
public class ShockersController : AuthenticatedSessionControllerBase
{
    private readonly ShockLinkContext _db;
    
    public ShockersController(ShockLinkContext db)
    {
        _db = db;
    }

    [HttpGet("{id:guid}/shockers")]
    public async Task<BaseResponse<IEnumerable<ShockerResponse>>> GetShockers(Guid id)
    {
        var deviceExists = await _db.Devices.AnyAsync(x => x.Owner == CurrentUser.DbUser.Id && x.Id == id);
        if (!deviceExists) return EBaseResponse<IEnumerable<ShockerResponse>>("Device does not exists or you do not have access to it.", HttpStatusCode.NotFound);
        var shockers = await _db.Shockers.Where(x => x.Device == id).Select(x => new ShockerResponse()
        {
            Id = x.Id,
            Name = x.Name,
            RfId = x.RfId,
            CreatedOn = x.CreatedOn
        }).ToListAsync();
        return new BaseResponse<IEnumerable<ShockerResponse>>
        {
            Message = "Successfully created shocker",
            Data = shockers
        };
    }
}