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
[Route("/{version:apiVersion}/sessions"), ApiVersion("1")]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemes.UserSessionCookie)]
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