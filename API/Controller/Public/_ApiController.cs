using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.ServicesCommon;
using Redis.OM.Contracts;
using Redis.OM.Searching;

namespace OpenShock.API.Controller.Public;

[ApiController]
[Route("/{version:apiVersion}/public")]
[AllowAnonymous]
public sealed partial class PublicController : OpenShockControllerBase
{
    private readonly OpenShockContext _db;
    private readonly IRedisConnectionProvider _redis;
    private readonly ILogger<PublicController> _logger;

    public PublicController(OpenShockContext db, IRedisConnectionProvider redis, ILogger<PublicController> logger)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
    }
}