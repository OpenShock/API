using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.ShockLinkDb;
using OpenShock.ServicesCommon.Authentication;

namespace OpenShock.API.Controller.Shockers;

[ApiController]
[ApiVersion("1")]
[ApiVersion("2")]
[Route("/{version:apiVersion}/shockers")]
public sealed partial class ShockerController : AuthenticatedSessionControllerBase
{
    private readonly ShockLinkContext _db;

    public ShockerController(ShockLinkContext db)
    {
        _db = db;
    }
}