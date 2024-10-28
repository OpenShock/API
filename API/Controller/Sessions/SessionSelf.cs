﻿using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Response;
using OpenShock.Common.Authentication.Attributes;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.Problems;

namespace OpenShock.API.Controller.Sessions;

public sealed partial class SessionsController
{
    /// <summary>
    /// Gets information about the current token used to access this endpoint
    /// </summary>
    /// <param name="userReferenceService"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    [HttpGet("self")]
    [UserSessionOnly]
    [ProducesSlimSuccess<LoginSessionResponse>]
    public LoginSessionResponse GetSelfSession([FromServices] IUserReferenceService userReferenceService)
    {
        var x = userReferenceService.AuthReference;
        
        if (x == null) throw new Exception("This should not be reachable due to AuthenticatedSession requirement");
        if (!x.Value.IsT0) throw new Exception("This should not be reachable due to the [UserSessionOnly] attribute");
        
        var session = x.Value.AsT0;
        
        return new LoginSessionResponse
        {
            Id = session.PublicId!.Value,
            Created = session.Created!.Value,
            Expires = session.Expires!.Value,
            Ip = session.Ip,
            UserAgent = session.UserAgent
        };
    }
}