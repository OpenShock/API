using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.OpenShockDb;
using OpenShock.ServicesCommon.Authentication;

namespace OpenShock.API.Controller.Device;

[ApiController]
[Route("/{version:apiVersion}/device")]
public sealed partial class DeviceController : AuthenticatedDeviceControllerBase
{
    private readonly OpenShockContext _db;
    private readonly ILogger<DeviceController> _logger;

    public DeviceController(OpenShockContext db, ILogger<DeviceController> logger)
    {
        _db = db;
        _logger = logger;
    }
}