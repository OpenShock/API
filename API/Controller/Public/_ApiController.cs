using Microsoft.AspNetCore.Mvc;
using OpenShock.Common;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Public;

[ApiController]
[Route("/{version:apiVersion}/public")]
public sealed partial class PublicController : OpenShockControllerBase
{
    private readonly OpenShockContext _db;
    private readonly ILogger<PublicController> _logger;

    public PublicController(OpenShockContext db, ILogger<PublicController> logger)
    {
        _db = db;
        _logger = logger;
    }
}
