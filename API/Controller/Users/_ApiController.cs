using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Authentication;
using OpenShock.Common.Authentication.ControllerBase;
using OpenShock.Common.OpenShockDb;
using Redis.OM.Contracts;

namespace OpenShock.API.Controller.Users;

[ApiController]
[Tags("Users")]
[Route("/{version:apiVersion}/users")]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemas.UserSessionApiTokenCombo)]
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