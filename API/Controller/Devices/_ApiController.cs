using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.OpenShockDb;
using OpenShock.ServicesCommon.Authentication;

namespace OpenShock.API.Controller.Devices;

/// <summary>
/// Device management
/// </summary>
[ApiController]
[Route("/{version:apiVersion}/devices")]
public sealed partial class DevicesController : AuthenticatedSessionControllerBase
{
    private readonly OpenShockContext _db;
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(OpenShockContext db, ILogger<DevicesController> logger)
    {
        _db = db;
        _logger = logger;
    }
}