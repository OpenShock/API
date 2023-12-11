using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.OpenShockDb;
using OpenShock.ServicesCommon.Authentication;
using Redis.OM.Contracts;

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
    private readonly IRedisConnectionProvider _redis;
    private readonly ILogger<ShockerController> _logger;

    public ShockerController(OpenShockContext db, IRedisConnectionProvider redis, ILogger<ShockerController> logger)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
    }
}