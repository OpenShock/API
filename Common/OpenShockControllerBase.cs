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
    protected string GetCookieDomains()
    {
        return HttpContext.RequestServices.GetRequiredService<IOptions<FrontendOptions>>().Value.CookieDomain;
    }

    [NonAction]
    protected string? GetCurrentCookieDomain()
    {
        return DomainUtils.GetBestMatchingCookieDomain(HttpContext.Request.Host.Host, GetCookieDomains());
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
        var domains = GetCookieDomains().AsSpan();
        foreach (var range in domains.Split(','))
        {
            var domain = domains[range];
            if (!DomainUtils.IsValidDomain(domain)) continue;

            var domainStr = "." + domain.ToString();
            
            HttpContext.Response.Cookies.Append(AuthConstants.UserSessionCookieName, string.Empty, GetCookieOptions(domainStr, TimeSpan.FromDays(-1)));
        }
    }
}