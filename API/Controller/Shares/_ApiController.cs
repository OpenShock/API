using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Authentication;
using OpenShock.Common.Authentication.ControllerBase;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Shares;

/// <summary>
/// Shocker share management
/// </summary>
[ApiController]
[Tags("Shocker Shares")]
[ApiVersion("1")]
[ApiVersion("2")]
[Route("/{version:apiVersion}/shares")]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemas.UserSessionApiTokenCombo)]
public sealed partial class SharesController : AuthenticatedSessionControllerBase
{
    private readonly OpenShockContext _db;

    public SharesController(OpenShockContext db)
    {
        _db = db;
    }
}