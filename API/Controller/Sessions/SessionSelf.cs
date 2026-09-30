using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Response;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.Redis;

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
    public LoginSessionResponse GetSelfSession([FromServices] IUserReferenceService userReferenceService)
    {
        if (userReferenceService.AuthReference is not LoginSession session)
            throw new UnreachableException("the [UserSessionOnly] attribute should have blocked caller");
        
        return LoginSessionResponse.MapFrom(session);
    }
}