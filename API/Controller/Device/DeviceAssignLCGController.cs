using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.ServicesCommon.Authentication;
using OpenShock.ServicesCommon.Geo;

namespace OpenShock.API.Controller.Device;

[ApiController]
[Route("/{version:apiVersion}/device/assignLCG")]
public sealed class DeviceAssignLcgController : AuthenticatedDeviceControllerBase
{
    private readonly OpenShockContext _db;
    private readonly IGeoLocation _geoLocation;

    public DeviceAssignLcgController(OpenShockContext db, IGeoLocation geoLocation)
    {
        _db = db;
        _geoLocation = geoLocation;
    }

    [HttpGet]
    public async Task<BaseResponse<object>> Get()
    {
        await _geoLocation.GetClosestNode(HttpContext.Connection.LocalIpAddress!);
        return new BaseResponse<object>();
    }
}