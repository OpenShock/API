using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Response;
using OpenShock.Common.Authentication.Services;

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
        var x = userReferenceService.AuthReference;
        
        if (x is null) throw new UnreachableException("AuthenticatedSession requirement");
        if (!x.Value.IsT0) throw new UnreachableException("the [UserSessionOnly] attribute");
        
        var session = x.Value.AsT0;
        
        return LoginSessionResponse.MapFrom(session);
    }
}