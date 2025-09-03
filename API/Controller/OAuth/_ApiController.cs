using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Services.Account;
using OpenShock.API.Services.OAuth;
using OpenShock.Common;

namespace OpenShock.API.Controller.OAuth;

/// <summary>
/// OAuth management
/// </summary>
[ApiController]
[Tags("OAuth")]
[ApiVersion("1")]
[Route("/{version:apiVersion}/oauth")]
public sealed partial class OAuthController : OpenShockControllerBase
{
    private readonly IAccountService _accountService;
    private readonly IOAuthHandlerRegistry _registry;
    private readonly ILogger<OAuthController> _logger;

    public OAuthController(IAccountService accountService, IOAuthHandlerRegistry registry, ILogger<OAuthController> logger)
    {
        _accountService = accountService;
        _registry = registry;
        _logger = logger;
    }
}