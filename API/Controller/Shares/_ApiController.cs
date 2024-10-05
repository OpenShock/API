using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Authentication.ControllerBase;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Shares;

/// <summary>
/// Shocker share management
/// </summary>
[ApiController]
[Route("/{version:apiVersion}/shares")]
public sealed partial class SharesController : AuthenticatedSessionControllerBase
{
    private readonly OpenShockContext _db;

    public SharesController(OpenShockContext db)
    {
        _db = db;
    }
}