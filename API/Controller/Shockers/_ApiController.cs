using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Authentication.ControllerBase;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Shockers;

/// <summary>
/// Shocker management
/// </summary>
[ApiController]
[ApiVersion("1")]
[ApiVersion("2")]
[Route("/{version:apiVersion}/shockers")]
public sealed partial class ShockerController : AuthenticatedSessionControllerBase
{
    private readonly OpenShockContext _db;
    private readonly ILogger<ShockerController> _logger;

    public ShockerController(OpenShockContext db, ILogger<ShockerController> logger)
    {
        _db = db;
        _logger = logger;
    }
}