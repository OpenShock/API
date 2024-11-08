using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Services.Session;
using OpenShock.Common.Authentication.Attributes;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.Problems;
using OpenShock.Common.Utils;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    [HttpPost("logout")]
    [ProducesSlimSuccess]
    [MapToApiVersion("1")]
    public async Task<IActionResult> Logout(
        [FromServices] ISessionService sessionService,
        [FromServices] ApiConfig apiConfig)
    {
        // Remove session if valid
        if (HttpContext.TryGetSessionKey(out var sessionKey))
        {
            await sessionService.DeleteSessionById(sessionKey);
        }
        
        // Make sure cookie is removed, no matter if authenticated or not
        var cookieDomainToUse = apiConfig.Frontend.CookieDomain.Split(',').FirstOrDefault(domain => Request.Headers.Host.ToString().EndsWith(domain, StringComparison.OrdinalIgnoreCase));
        if (cookieDomainToUse != null)
        {
            HttpContext.RemoveSessionKeyCookie("." + cookieDomainToUse);
        }
        else // Fallback to all domains
        {
            foreach (var domain in apiConfig.Frontend.CookieDomain.Split(','))
            {
                HttpContext.RemoveSessionKeyCookie("." + domain);
            }
        }

        // its always a success, logout endpoints should be idempotent
        return Ok();
    }
}