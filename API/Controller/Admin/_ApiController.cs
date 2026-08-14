using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Authentication;
using OpenShock.Common.Authentication.ControllerBase;
using OpenShock.Common.OpenShockDb;
using Redis.OM.Contracts;

namespace OpenShock.API.Controller.Admin;

[ApiController]
[Tags("Admin")]
[EndpointGroupName("admin")]
[Route("/{version:apiVersion}/admin")]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemes.UserSessionCookie, Roles = "Admin")]
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

    /// <summary>
    /// Defense-in-depth admin gate, called first in every admin action. The controller is already
    /// restricted to the Admin role by <c>[Authorize(Roles = "Admin")]</c>, but this independently
    /// re-checks that requirement against the freshly DB-loaded
    /// <see cref="AuthenticatedSessionControllerBase.CurrentUser"/> (the same record the session's role
    /// claims are built from). If that attribute is ever loosened, removed, or bypassed, the request
    /// fails closed here.
    /// </summary>
    /// <remarks>
    /// It throws (yielding a 500) rather than returning a typed 403 on purpose: reaching an admin action
    /// without actually holding the Admin role is a "should never happen" condition, and failing hard is
    /// the correct, safe outcome, far better than leaking an admin operation.
    /// </remarks>
    private void PedanticallyEnsureAdmin()
    {
        if (!CurrentUser.Roles.Contains(RoleType.Admin))
        {
            throw new Exception($"PedanticallyEnsureAdmin failed: user {CurrentUser.Id} reached an admin endpoint without the Admin role");
        }
    }
}