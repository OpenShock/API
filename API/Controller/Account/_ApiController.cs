using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common;
using OpenShock.API.Services.Account;

namespace OpenShock.API.Controller.Account;

/// <summary>
/// User account management
/// </summary>
[ApiController]
[Tags("Account")]
[ApiVersion("1"), ApiVersion("2")]
[Route("/{version:apiVersion}/account")]
public sealed partial class AccountController : OpenShockControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IAccountService accountService, ILogger<AccountController> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }
}