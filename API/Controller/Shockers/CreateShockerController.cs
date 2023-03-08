using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShockLink.API.Authentication;
using ShockLink.API.Models;
using ShockLink.API.Models.Requests;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller.Shockers;

[ApiController]
[Route("/{version:apiVersion}/shockers")]
public class CreateShockersController : AuthenticatedSessionControllerBase
{
    private readonly ShockLinkContext _db;
    
    public CreateShockersController(ShockLinkContext db)
    {
        _db = db;
    }
    
    [HttpPost]
    public async Task<BaseResponse<Guid>> CreateShocker(NewShocker data)
    {
        var device = await _db.Devices.AnyAsync(x => x.Owner == CurrentUser.DbUser.Id && x.Id == data.Device);
        if(!device) return EBaseResponse<Guid>("Device does not exist", HttpStatusCode.NotFound);
        
        var shocker = new Shocker
        {
            Id = Guid.NewGuid(),
            Name = data.Name,
            RfId = data.RfId,
            Device = data.Device
        };
        _db.Shockers.Add(shocker);
        await _db.SaveChangesAsync();

        Response.StatusCode = (int)HttpStatusCode.Created;
        return new BaseResponse<Guid>
        {
            Data = shocker.Id
        };
    }
}