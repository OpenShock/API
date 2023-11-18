using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.OpenShockDb;
using OpenShock.ServicesCommon;
using Redis.OM.Contracts;

namespace OpenShock.API.Controller.Account;

/// <summary>
/// User account management
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("/{version:apiVersion}/account")]
public sealed partial class AccountController : OpenShockControllerBase
{
    private readonly OpenShockContext _db;
    private readonly IRedisConnectionProvider _redis;
    private readonly ILogger<AccountController> _logger;

    public AccountController(OpenShockContext db, IRedisConnectionProvider redis, ILogger<AccountController> logger)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
    }
}