using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Authentication;
using OpenShock.Common.Authentication.ControllerBase;
using OpenShock.Common.OpenShockDb;
using Redis.OM.Contracts;

namespace OpenShock.API.Controller.Admin;

[ApiController]
[Route("/{version:apiVersion}/admin")]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemas.UserSessionCookie, Policy = OpenShockAuthPolicies.AdminAccess)]
public sealed partial class AdminController : AuthenticatedSessionControllerBase
{
    private readonly OpenShockContext _db;
    private readonly IRedisConnectionProvider _redis;
    private readonly ILogger<AdminController> _logger;

    public AdminController(OpenShockContext db, IRedisConnectionProvider redis, ILogger<AdminController> logger)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
    }
}