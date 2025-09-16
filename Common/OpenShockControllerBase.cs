using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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

    private static CookieOptions GetCookieOptions(string domain, TimeSpan lifetime)
    {
        return new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.Add(lifetime),
            Secure = true,
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Domain = domain
        };
    }

    [NonAction]
    protected async Task CreateSession(Guid accountId, string domain)
    {
        var sessionService = HttpContext.RequestServices.GetRequiredService<ISessionService>();
        
        var session = await sessionService.CreateSessionAsync(accountId, HttpContext.GetUserAgent(), HttpContext.GetRemoteIP().ToString());
        
        HttpContext.Response.Cookies.Append(AuthConstants.UserSessionCookieName, session.Token, GetCookieOptions(domain, Duration.LoginSessionLifetime));
    }

    [NonAction]
    protected void RemoveSessionKeyCookie()
    {
        var cookieDomains = HttpContext.RequestServices.GetRequiredService<FrontendOptions>().CookieDomains;
        foreach (var domain in cookieDomains)
        {
            HttpContext.Response.Cookies.Append(AuthConstants.UserSessionCookieName, string.Empty, GetCookieOptions(domain, TimeSpan.FromDays(-1)));
        }
    }
}