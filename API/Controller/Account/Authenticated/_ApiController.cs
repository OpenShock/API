using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Services.Account;
using OpenShock.Common.Authentication;
using OpenShock.Common.Authentication.ControllerBase;

namespace OpenShock.API.Controller.Account.Authenticated;

/// <summary>
/// User account management
/// </summary>
[ApiController]
[Tags("Account")]
[ApiVersion("1")]
[Route("/{version:apiVersion}/account")]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemes.UserSessionCookie)]
public sealed partial class AuthenticatedAccountController : AuthenticatedSessionControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ILogger<AuthenticatedAccountController> _logger;

    public AuthenticatedAccountController(
        IAccountService accountService,
        ILogger<AuthenticatedAccountController> logger
        )
    {
        _accountService = accountService;
        _logger = logger;
    }
}