using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.OpenShockDb;
using OpenShock.ServicesCommon.Authentication.ControllerBase;
using Redis.OM.Contracts;

namespace OpenShock.API.Controller.Users;

[ApiController]
[Route("/{version:apiVersion}/users")]
public sealed partial class UsersController : AuthenticatedSessionControllerBase
{
    private readonly OpenShockContext _db;
    private readonly IRedisConnectionProvider _redis;
    private readonly ILogger<UsersController> _logger;

    public UsersController(OpenShockContext db, IRedisConnectionProvider redis, ILogger<UsersController> logger)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
    }
}