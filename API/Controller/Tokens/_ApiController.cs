using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Authentication;
using OpenShock.Common.Authentication.ControllerBase;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Tokens;

[ApiController]
[Tags("API Tokens")]
[Route("/{version:apiVersion}/tokens")]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemes.UserSessionCookie)]
public sealed partial class TokensController : AuthenticatedSessionControllerBase
{
    private readonly OpenShockContext _db;
    private readonly ILogger<TokensController> _logger;

    public TokensController(OpenShockContext db, ILogger<TokensController> logger)
    {
        _db = db;
        _logger = logger;
    }
}