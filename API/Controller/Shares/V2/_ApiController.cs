using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Authentication.ControllerBase;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Shares.V2;

/// <summary>
/// Shocker share management
/// </summary>
[ApiController]
[Route("/{version:apiVersion}/shares")]
[ApiVersion("2")]
public sealed partial class SharesV2Controller : AuthenticatedSessionControllerBase
{
    private readonly OpenShockContext _db;

    public SharesV2Controller(OpenShockContext db)
    {
        _db = db;
    }
}