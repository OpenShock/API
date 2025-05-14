using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Response;
using OpenShock.Common.Authentication;
using OpenShock.Common.Authentication.ControllerBase;
using OpenShock.Common.Authentication.Services;

namespace OpenShock.API.Controller.Tokens;

[ApiController]
[Tags("API Tokens")]
[Route("/{version:apiVersion}/tokens")]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemas.ApiToken)]
public sealed partial class TokensSelfController : AuthenticatedSessionControllerBase
{
    /// <summary>
    /// Gets information about the current token used to access this endpoint
    /// </summary>
    /// <param name="userReferenceService"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    [HttpGet("self")]
    public TokenResponse GetSelfToken([FromServices] IUserReferenceService userReferenceService)
    {
        var x = userReferenceService.AuthReference;
        
        if (x == null) throw new Exception("This should not be reachable due to AuthenticatedSession requirement");
        if (!x.Value.IsT1) throw new Exception("This should not be reachable due to the [TokenOnly] attribute");
        
        var token = x.Value.AsT1;
        
        return new TokenResponse
        {
            CreatedOn = token.CreatedAt,
            ValidUntil = token.ValidUntil,
            LastUsed = token.LastUsed,
            Permissions = token.Permissions,
            Name = token.Name,
            Id = token.Id
        };
    }
}