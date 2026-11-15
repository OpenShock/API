using Microsoft.AspNetCore.Mvc;
using OpenShock.Common;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Integrations;

/// <summary>
/// Endpoints for first party services (e.g. the Discord bot). Not tied to a user session, every action checks
/// for an automation token that grants the capability it needs.
/// </summary>
[ApiController]
[Tags("Integrations")]
[Route("/{version:apiVersion}/integrations")]
public sealed partial class IntegrationsController : OpenShockControllerBase
{
    private readonly OpenShockContext _db;

    public IntegrationsController(OpenShockContext db)
    {
        _db = db;
    }
}
