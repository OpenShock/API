using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Authentication.ControllerBase;
using OpenShock.Common.OpenShockDb;
using Redis.OM.Contracts;

namespace OpenShock.API.Controller.Devices;

/// <summary>
/// Device management
/// </summary>
[ApiController]
[Route("/{version:apiVersion}/devices")]
public sealed partial class DevicesController : AuthenticatedSessionControllerBase
{
    private readonly OpenShockContext _db;
    private readonly IRedisConnectionProvider _redis;
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(OpenShockContext db, IRedisConnectionProvider redis, ILogger<DevicesController> logger)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
    }
}