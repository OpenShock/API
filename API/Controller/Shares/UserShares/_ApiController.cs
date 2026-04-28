using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Authentication;
using OpenShock.Common.Authentication.ControllerBase;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Shares.UserShares;

/// <summary>
/// Shocker share management
/// </summary>
[ApiController]
[Tags("User Shocker Shares")]
[ApiVersion("2")]
[Route("/{version:apiVersion}/shares/user")]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemes.UserSessionCookie)]
public sealed partial class UserSharesController : AuthenticatedSessionControllerBase
{
    private readonly OpenShockContext _db;

    public UserSharesController(OpenShockContext db)
    {
        _db = db;
    }
}