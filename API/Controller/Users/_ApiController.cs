using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.ServicesCommon.Authentication;

namespace OpenShock.API.Controller.Users;

[ApiController]
[Route("/{version:apiVersion}/users")]
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