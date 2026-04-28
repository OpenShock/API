using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;
using OpenShock.Common.Options;
using OpenShock.Common.Problems;
using OpenShock.Common.Services.Session;
using OpenShock.Common.Utils;

namespace OpenShock.Common;

public class OpenShockControllerBase : ControllerBase
{
    [NonAction]
    protected ObjectResult Problem(OpenShockProblem problem) => problem.ToObjectResult(HttpContext);
    
    [NonAction]
    protected OkObjectResult LegacyDataOk<T>(T data, string message = "")
    {
        return Ok(new LegacyDataResponse<T>(data, message));
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
    protected string? GetCurrentCookieDomain()
    {
        var cookieDomains = HttpContext.RequestServices.GetRequiredService<FrontendOptions>().CookieDomains;
        return DomainUtils.GetBestMatchingCookieDomain(HttpContext.Request.Host.Host, cookieDomains);
    }

    [NonAction]
    protected async Task CreateSession(Guid accountId, string domain)
    {
        var sessionService = HttpContext.RequestServices.GetRequiredService<ISessionService>();
        
        var session = await sessionService.CreateSessionAsync(accountId, HttpContext.GetUserAgent(), HttpContext.GetRemoteIP().ToString());
        
        HttpContext.Response.Cookies.Append(AuthConstants.UserSessionCookieName, session.Token, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.Add(Duration.LoginSessionLifetime),
            Secure = true,
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Domain = domain
        });
    }

    [NonAction]
    protected void RemoveSessionKeyCookie()
    {
        var cookieDomains = HttpContext.RequestServices.GetRequiredService<FrontendOptions>().CookieDomains;
        foreach (var domain in cookieDomains)
        {
            HttpContext.Response.Cookies.Delete(AuthConstants.UserSessionCookieName, new CookieOptions { Domain = domain });
        }
    }
}