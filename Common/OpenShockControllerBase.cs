using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Authentication;
using OpenShock.Common.Models;
using OpenShock.Common.Problems;

namespace OpenShock.Common;

[Consumes(MediaTypeNames.Application.Json)]
public class OpenShockControllerBase : ControllerBase
{
    [NonAction]
    protected ObjectResult Problem(OpenShockProblem problem) => problem.ToObjectResult(HttpContext);
    
    [NonAction]
    protected OkObjectResult LegacyDataOk<T>(T data)
    {
        return Ok(new LegacyDataResponse<T>(data));
    }

    [NonAction]
    protected CreatedResult LegacyDataCreated<T>(string? uri, T data)
    {
        return Created(uri, new LegacyDataResponse<T>(data));
    }

    [NonAction]
    protected OkObjectResult LegacyEmptyOk(string message = "")
    {
        return Ok(new LegacyEmptyResponse(message));
    }
    
    [NonAction]
    protected bool TryGetOpenShockUserIdentity([NotNullWhen(true)] out ClaimsIdentity? identity)
    {
        foreach (var ident in User.Identities)
        {
            if (!ident.IsAuthenticated) continue;
            
            foreach (var claim in ident.Claims)
            {
                if (claim is
                    {
                        Type: ClaimTypes.AuthenticationMethod,
                        Value: OpenShockAuthSchemes.UserSessionCookie
                    })
                {
                    identity = ident;
                    return true;
                }
            }
        }

        identity = null;
        return false;
    }

    [NonAction]
    protected bool IsOpenShockUserAuthenticated()
    {
        foreach (var ident in User.Identities)
        {
            if (!ident.IsAuthenticated) continue;
            
            foreach (var claim in ident.Claims)
            {
                if (claim is
                    {
                        Type: ClaimTypes.AuthenticationMethod,
                        Value: OpenShockAuthSchemes.UserSessionCookie
                    })
                {
                    return true;
                }
            }
        }

        return false;
    }

    [NonAction]
    protected bool TryGetAuthenticatedOpenShockUserId(out Guid userId)
    {
        if (!TryGetOpenShockUserIdentity(out var identity))
        {
            userId = Guid.Empty;
            return false;
        }
        
        var idStr = identity.Claims.SingleOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(idStr))
        {
            userId = Guid.Empty;
            return false;
        }

        return Guid.TryParse(idStr, out userId);

    }
}