using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Authentication;
using OpenShock.Common.Authentication.ControllerBase;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Users;

[ApiController]
[Tags("Users")]
[Route("/{version:apiVersion}/users")]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemes.UserSessionApiTokenCombo)]
public sealed partial class UsersController : AuthenticatedSessionControllerBase
{
    private readonly OpenShockContext _db;
    private readonly ILogger<UsersController> _logger;

    public UsersController(OpenShockContext db, ILogger<UsersController> logger)
    {
        _db = db;
        _logger = logger;
    }
}