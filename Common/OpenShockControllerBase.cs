using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Options;
using OpenShock.Common.Services.Session;
using OpenShock.Common.Utils;

using OpenShock.Internal.Common.Problems;

namespace OpenShock.Common;

// Inherits Problem(OpenShockProblem) from OpenShock.Internal.Common.OpenShockControllerBase;
// the members below are OpenShock-API-specific and stay local.
public class OpenShockControllerBase : OpenShock.Internal.Common.OpenShockControllerBase
{
    [NonAction]
    protected T GetRequiredItem<T>() where T : class
    {
        var key = typeof(T).Name;
        
        if (!HttpContext.Items.TryGetValue(key, out var value))
        {
            throw new InvalidOperationException($"HttpContext.Items does not contain a required item of type {key}");
        }

        if (value is null)
        {
            throw new InvalidOperationException($"HttpContext.Items contains the required item but it is null (expected: {typeof(T).FullName}).");
        }

        if (value is not T typed)
        {
            throw new InvalidOperationException($"HttpContext.Items[\"{key}\"] is of type {value.GetType().FullName}, but an instance of {typeof(T).FullName} was expected.");
        }
        
        return typed;
    }
    
    /// <summary>
    /// The item if present and of type <typeparamref name="T"/>, otherwise null. Use for items that
    /// only exist for some authentication schemes, e.g. ApiToken on a session-authenticated request.
    /// </summary>
    [NonAction]
    protected T? GetOptionalItem<T>() where T : class
        => HttpContext.Items.TryGetValue(typeof(T).Name, out var value) ? value as T : null;

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

        var remoteIp = HttpContext.GetRemoteIP();
        var userAgent = HttpContext.GetUserAgent();

        var session = await sessionService.CreateSessionAsync(accountId, userAgent, remoteIp.ToString(), actorId: accountId);

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