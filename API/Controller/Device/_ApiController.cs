using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Authentication;
using OpenShock.Common.Authentication.ControllerBase;
using OpenShock.Common.OpenShockDb;
using Redis.OM.Contracts;

namespace OpenShock.API.Controller.Device;

/// <summary>
/// For devices (ESP's)
/// </summary>
[ApiController]
[ApiVersion("1")]
[ApiVersion("2")]
[Tags("Hub Endpoints")]
[Route("/{version:apiVersion}/device")]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemas.HubToken)]
public sealed partial class DeviceController : AuthenticatedHubControllerBase
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