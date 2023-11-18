using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.OpenShockDb;
using OpenShock.ServicesCommon.Authentication;
using Redis.OM.Contracts;

namespace OpenShock.API.Controller.Tokens;

[ApiController]
[Route("/{version:apiVersion}/tokens")]
public sealed partial class TokensController : AuthenticatedSessionControllerBase
{
    private readonly OpenShockContext _db;
    private readonly IRedisConnectionProvider _redis;
    private readonly ILogger<TokensController> _logger;

    public TokensController(OpenShockContext db, IRedisConnectionProvider redis, ILogger<TokensController> logger)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
    }
}