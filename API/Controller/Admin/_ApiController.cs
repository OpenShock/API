﻿using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.ServicesCommon.Authentication.Attributes;
using OpenShock.ServicesCommon.Authentication.ControllerBase;
using Redis.OM.Contracts;

namespace OpenShock.API.Controller.Admin;

[ApiController]
[Rank(RankType.Admin)]
[UserSessionOnly]
[Route("/{version:apiVersion}/admin")]
public sealed partial class AdminController : AuthenticatedSessionControllerBase
{
    private readonly OpenShockContext _db;
    private readonly IRedisConnectionProvider _redis;
    private readonly ILogger<AdminController> _logger;

    public AdminController(OpenShockContext db, IRedisConnectionProvider redis, ILogger<AdminController> logger)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
    }
}