using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Authentication.Attributes;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.Problems;

namespace OpenShock.API.Controller.Account.Authenticated;

public sealed partial class AuthenticatedAccountController
{
    [HttpDelete("logout")]
    [UserSessionOnly]
    [ProducesSlimSuccess]
    public async Task<IActionResult> Logout(
        [FromServices] IUserReferenceService userReferenceService,
        [FromServices] ApiConfig apiConfig)
    {
        var x = userReferenceService.AuthReference;
        
        if (x == null) throw new Exception("This should not be reachable due to AuthenticatedSession requirement");
        if (!x.Value.IsT0) throw new Exception("This should not be reachable due to the [UserSessionOnly] attribute");
        
        var session = x.Value.AsT0;
        
        await _sessionService.DeleteSession(session);
        
        var cookieDomainToUse = apiConfig.Frontend.CookieDomain.Split(',').FirstOrDefault(domain => Request.Headers.Host.ToString().EndsWith(domain, StringComparison.OrdinalIgnoreCase));
        if (cookieDomainToUse != null)
        {
            HttpContext.Response.Cookies.Append("openShockSession", string.Empty, new CookieOptions
            {
                Expires = DateTimeOffset.FromUnixTimeSeconds(0),
                Secure = true,
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Domain = "." + cookieDomainToUse
            });
        }
        else // Fallback to all domains
        {
            foreach (var stringValue in apiConfig.Frontend.CookieDomain.Split(','))
            {
                HttpContext.Response.Cookies.Append("openShockSession", string.Empty, new CookieOptions
                {
                    Expires = DateTimeOffset.FromUnixTimeSeconds(0),
                    Secure = true,
                    HttpOnly = true,
                    SameSite = SameSiteMode.Strict,
                    Domain = "." + stringValue
                });
            }
        }

        return RespondSlimSuccess();
    }
}