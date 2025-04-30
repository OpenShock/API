using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Authentication;
using OpenShock.Common.Authentication.ControllerBase;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Shares.Links;

/// <summary>
/// Share links management
/// </summary>
[ApiController]
[Tags("Shocker ShareLinks")]
[Route("/{version:apiVersion}/shares/links")]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemas.UserSessionApiTokenCombo)]
public sealed partial class ShareLinksController : AuthenticatedSessionControllerBase
{
    private readonly OpenShockContext _db;

    public ShareLinksController(OpenShockContext db)
    {
        _db = db;
    }
}