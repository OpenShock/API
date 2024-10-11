using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common;
using OpenShock.API.Services.Account;
using OpenShock.Common.OpenShockDb;
using Redis.OM.Contracts;

namespace OpenShock.API.Controller.Account;

/// <summary>
/// User account management
/// </summary>
[ApiController]
[AllowAnonymous]
[ApiVersion("1")]
[ApiVersion("2")]
[Route("/{version:apiVersion}/account")]
public sealed partial class AccountController : OpenShockControllerBase
{
    private readonly OpenShockContext _db;
    private readonly IRedisConnectionProvider _redis;
    private readonly ILogger<Authenticated.AuthenticatedAccountController> _logger;
    private readonly IAccountService _accountService;

    public AccountController(OpenShockContext db, IRedisConnectionProvider redis, ILogger<Authenticated.AuthenticatedAccountController> logger, IAccountService accountService)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
        _accountService = accountService;
    }
}