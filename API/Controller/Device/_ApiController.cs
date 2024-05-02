using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.OpenShockDb;
using OpenShock.ServicesCommon.Authentication.ControllerBase;
using Redis.OM.Contracts;

namespace OpenShock.API.Controller.Device;

/// <summary>
/// For devices (ESP's)
/// </summary>
[ApiController]
[Route("/{version:apiVersion}/device")]
public sealed partial class DeviceController : AuthenticatedDeviceControllerBase
{
    private readonly OpenShockContext _db;
    private readonly IRedisConnectionProvider _redis;
    private readonly ILogger<DeviceController> _logger;

    public DeviceController(OpenShockContext db, IRedisConnectionProvider redis, ILogger<DeviceController> logger)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
    }
}