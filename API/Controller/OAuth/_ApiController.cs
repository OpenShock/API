using Asp.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Services.Account;
using OpenShock.Common;

namespace OpenShock.API.Controller.OAuth;

/// <summary>
/// OAuth management endpoints (provider listing, authorize, data handoff).
/// </summary>
[ApiController]
[Tags("OAuth")]
[ApiVersion("1")]
[Route("/{version:apiVersion}/oauth")]
public sealed partial class OAuthController : OpenShockControllerBase
{
    private readonly IAccountService _accountService;
    private readonly IAuthenticationSchemeProvider _schemeProvider;
    private readonly ILogger<OAuthController> _logger;

    public OAuthController(IAccountService accountService, IAuthenticationSchemeProvider schemeProvider, ILogger<OAuthController> logger)
    {
        _accountService = accountService;
        _schemeProvider = schemeProvider;
        _logger = logger;
    }
}