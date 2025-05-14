using Microsoft.AspNetCore.Mvc;
using OpenShock.Common;
using OpenShock.Common.OpenShockDb;
using Redis.OM.Contracts;

namespace OpenShock.API.Controller.Public;

[ApiController]
[Route("/{version:apiVersion}/public")]
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