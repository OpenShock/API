using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShockLink.API.Authentication;
using ShockLink.API.Models;
using ShockLink.API.Models.Response;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller.Device;

[ApiController]
[Route("/{version:apiVersion}/device/self")]
public class DeviceSelfController : AuthenticatedDeviceControllerBase
{
    private readonly ShockLinkContext _db;
    
    public DeviceSelfController(ShockLinkContext db)
    {
        _db = db;
    }
    
    [HttpGet]
    public async Task<BaseResponse<DeviceSelfResponse>> GetSelf()
    {
        var shockers = await _db.Shockers.Where(x => x.Device == CurrentDevice.Id).Select(x => new MinimalShocker
        {
            Id = x.Id,
            RfId = x.RfId,
            Model = x.Model
        }).ToArrayAsync();
        
        return new BaseResponse<DeviceSelfResponse>
        {
            Data = new DeviceSelfResponse
            {
                Shockers = shockers
            }
        };
    }
}