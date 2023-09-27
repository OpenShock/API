using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Response;
using OpenShock.Common.Models;
using OpenShock.Common.ShockLinkDb;
using OpenShock.ServicesCommon.Authentication;

namespace OpenShock.API.Controller.Device;

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
                Id = CurrentDevice.Id,
                Name = CurrentDevice.Name,
                Shockers = shockers
            }
        };
    }
}