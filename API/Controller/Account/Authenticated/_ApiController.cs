using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Services.Account;
using OpenShock.Common.Authentication.Attributes;
using OpenShock.Common.Authentication.ControllerBase;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.Session;
using Redis.OM.Contracts;

namespace OpenShock.API.Controller.Account.Authenticated;

/// <summary>
/// User account management
/// </summary>
[ApiController]
[UserSessionOnly]
[ApiVersion("1")]
[Route("/{version:apiVersion}/account")]
public sealed partial class AuthenticatedAccountController : AuthenticatedSessionControllerBase
{
    private readonly OpenShockContext _db;
    private readonly IRedisConnectionProvider _redis;
    private readonly ILogger<AuthenticatedAccountController> _logger;
    private readonly IAccountService _accountService;
    private readonly ISessionService _sessionService;

    public AuthenticatedAccountController(
        OpenShockContext db,
        IRedisConnectionProvider redis,
        ILogger<AuthenticatedAccountController> logger,
        IAccountService accountService,
        ISessionService sessionService)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
        _accountService = accountService;
        _sessionService = sessionService;
    }
}