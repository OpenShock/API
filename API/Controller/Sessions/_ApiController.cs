using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Services.Session;
using OpenShock.Common.Authentication.Attributes;
using OpenShock.Common.Authentication.ControllerBase;
using OpenShock.Common.Problems;

namespace OpenShock.API.Controller.Sessions;

/// <summary>
/// Session management
/// </summary>
[ApiController]
[UserSessionOnly]
[ApiVersion("1")]
[Route("/{version:apiVersion}/sessions")]
public sealed partial class SessionsController : AuthenticatedSessionControllerBase
{
    private readonly ISessionService _sessionService;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="sessionService"></param>
    public SessionsController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }
    

}