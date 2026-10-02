using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Constants;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Options;
using OpenShock.Common.Services.Geo;
using OpenShock.Common.Services.Session;
using OpenShock.Common.Utils;

using OpenShock.Internal.Common.Problems;

namespace OpenShock.Common;

// Inherits Problem(OpenShockProblem) from OpenShock.Internal.Common.OpenShockControllerBase;
// the members below are OpenShock-API-specific and stay local.
public abstract class OpenShockControllerBase : OpenShock.Internal.Common.OpenShockControllerBase
{
    /// <summary>
    /// Convenience wrapper for <see cref="OpenShock.Common.Extensions.HttpContextExtensions.GetRequiredItemByType{T}(HttpContext)"/>.
    /// </summary>
    [NonAction]
    protected T GetRequiredItem<T>() where T : class => HttpContext.GetRequiredItemByType<T>();

    /// <summary>
    /// Convenience wrapper for <see cref="OpenShock.Common.Extensions.HttpContextExtensions.GetItemByType{T}(HttpContext)"/>.
    /// </summary>
    [NonAction]
    protected T? GetOptionalItem<T>() where T : class => HttpContext.GetItemByType<T>();

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
        var frontendOptions = HttpContext.RequestServices.GetRequiredService<FrontendOptions>();
        var sessionService = HttpContext.RequestServices.GetRequiredService<ISessionService>();
        var enrichmentService = HttpContext.RequestServices.GetRequiredService<IIpEnrichmentService>();

        var remoteIp = HttpContext.GetRemoteIP();
        var userAgent = HttpContext.GetUserAgent();
        var enrichment = enrichmentService.Enrich(remoteIp);

        var session = await sessionService.CreateSessionAsync(accountId, userAgent, remoteIp, actorId: accountId, enrichment: enrichment);


        HttpContext.Response.Cookies.Append(AuthConstants.UserSessionCookieName, session.Token, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.Add(Duration.LoginSessionLifetime),
            // Either signal is enough to mark the cookie Secure: the request scheme is the authoritative
            // one (X-Forwarded-Proto is honoured for trusted proxies), and the configured frontend
            // scheme covers a proxy that fails to forward it. Only a plain-HTTP request against a
            // plain-HTTP frontend - dev and integration tests - yields a non-secure cookie.
            Secure = HttpContext.Request.IsHttps || frontendOptions.CookieSecure,
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