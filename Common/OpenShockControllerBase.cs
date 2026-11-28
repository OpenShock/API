using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Authentication;
using OpenShock.Common.Constants;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Options;
using OpenShock.Common.Services.Geo;
using OpenShock.Common.Services.Session;
using OpenShock.Common.Utils;

namespace OpenShock.Common;

// Inherits Problem(OpenShockProblem) from OpenShock.Internal.Common.OpenShockControllerBase;
// the members below are OpenShock-API-specific and stay local.
public abstract class OpenShockControllerBase : Internal.Common.OpenShockControllerBase
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
    protected OkObjectResult LegacyDataOk<T>(T data, string message = "") where T : notnull
    {
        return Ok(new LegacyDataResponse<T>(data, message));
    }

    [NonAction]
    protected CreatedResult LegacyDataCreated<T>(string? uri, T data) where T : notnull
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

    /// <param name="accountId"></param>
    /// <param name="domain"></param>
    /// <param name="securityStamp">
    /// The account's security stamp as it was when the caller checked the credential it is signing in with.
    /// If the stamp has changed by the time the session exists (a password reset completed in between, and
    /// its sign-out of all sessions ran before this one was created), the session is dropped again.
    /// </param>
    /// <returns>False when the session was dropped; nothing was set on the response.</returns>
    [NonAction]
    protected async Task<bool> CreateSession(Guid accountId, string domain, Guid? securityStamp = null)
    {
        var frontendOptions = HttpContext.RequestServices.GetRequiredService<FrontendOptions>();
        var sessionService = HttpContext.RequestServices.GetRequiredService<ISessionService>();
        var enrichmentService = HttpContext.RequestServices.GetRequiredService<IIpEnrichmentService>();

        var remoteIp = HttpContext.GetRemoteIP();
        var userAgent = HttpContext.GetUserAgent();
        var enrichment = enrichmentService.Enrich(remoteIp);

        var session = await sessionService.CreateSessionAsync(accountId, userAgent, remoteIp, actorId: accountId, enrichment: enrichment);

        if (securityStamp is not null)
        {
            // Checked after the session exists: a reset that commits later than this still finds the
            // session and deletes it, one that committed earlier shows up here.
            var db = HttpContext.RequestServices.GetRequiredService<OpenShockContext>();
            Guid? currentStamp;
            try
            {
                currentStamp = await db.Users.Where(u => u.Id == accountId).Select(u => (Guid?)u.SecurityStamp).FirstOrDefaultAsync();
            }
            catch
            {
                try
                {
                    await sessionService.DeleteSessionByIdAsync(session.Id);
                }
                catch (Exception cleanupException)
                {
                    HttpContext.RequestServices.GetRequiredService<ILogger<OpenShockControllerBase>>()
                        .LogError(cleanupException, "Failed to clean up session {SessionId} after the security-stamp check failed", session.Id);
                }

                throw;
            }
            if (currentStamp != securityStamp)
            {
                await sessionService.DeleteSessionByIdAsync(session.Id);
                return false;
            }
        }

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

        // Signed in, so the post-signup "didn't get the email?" cookie has served its purpose.
        ActivationResendCookie.Remove(HttpContext);

        return true;
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
