using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Authentication;
using OpenShock.Common.Authentication.ControllerBase;
using OpenShock.Common.Services.Session;

namespace OpenShock.API.Controller.Sessions;

/// <summary>
/// Session management
/// </summary>
[ApiController]
[Tags("Sessions")]
[ApiVersion("1")]
[Route("/{version:apiVersion}/sessions")]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemes.UserSessionCookie)]
public sealed partial class SessionsController : AuthenticatedSessionControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly ILogger<SessionsController> _logger;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="sessionService"></param>
    /// <param name="logger"></param>
    public SessionsController(ISessionService sessionService, ILogger<SessionsController> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }
    

}