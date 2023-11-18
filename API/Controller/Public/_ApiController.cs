using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.ServicesCommon;
using Redis.OM.Contracts;
using Redis.OM.Searching;

namespace OpenShock.API.Controller.Public;

[ApiController]
[Route("/{version:apiVersion}/public")]
[AllowAnonymous]
public sealed partial class PublicController : OpenShockControllerBase
{
    private readonly OpenShockContext _db;

    public PublicController(OpenShockContext db)
    {
        _db = db;
    }
}