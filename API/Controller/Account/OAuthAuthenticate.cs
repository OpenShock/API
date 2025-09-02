using Asp.Versioning;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using OpenShock.API.Models.Requests;
using OpenShock.API.Models.Response;
using OpenShock.API.Services.Account;
using OpenShock.Common.Errors;
using OpenShock.Common.Models;
using OpenShock.Common.Options;
using OpenShock.Common.Problems;
using OpenShock.Common.Utils;
using System.Net.Mime;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Warning: This endpoint is not meant to be called by API clients, but only by the frontend.
    /// SSO authentication endpoint
    /// </summary>
    /// <param name="providerName">Name of the SSO provider to use, supported providers can be fetched from /api/v1/sso/providers</param>
    /// <status code="406">Not Acceptable, the SSO provider is not supported</status>
    [EnableRateLimiting("auth")]
    [EnableCors("allow_sso_providers")]
    [HttpGet("oauth/{providerName}", Name = "InternalSsoAuthenticate")]
    [HttpPost("oauth/{providerName}", Name = "InternalSsoCallback")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status406NotAcceptable)]
    public async Task<IActionResult> OAuthAuthenticate(
        [FromBody] DiscordOAuth body,
        [FromServices] IOptions<FrontendOptions> options,
        CancellationToken cancellationToken)
    {
        var cookieDomainToUse = options.Value.CookieDomain.Split(',').FirstOrDefault(domain => Request.Headers.Host.ToString().EndsWith(domain, StringComparison.OrdinalIgnoreCase));
        if (cookieDomainToUse is null) return Problem(LoginError.InvalidDomain);

        var remoteIP = HttpContext.GetRemoteIP();

        var loginAction = await _accountService.CreateUserLoginSessionViaDiscordAsync(body.Code, new LoginContext
        {
            Ip = remoteIP.ToString(),
            UserAgent = HttpContext.GetUserAgent(),
        }, cancellationToken);

        return loginAction.Match<IActionResult>(
            ok =>
            {
                HttpContext.SetSessionKeyCookie(ok.Token, "." + cookieDomainToUse);
                return Ok(LoginV2OkResponse.FromUser(ok.User));
            },
            notActivated => Problem(AccountError.AccountNotActivated),
            deactivated => Problem(AccountError.AccountDeactivated),
            _ => Problem(LoginError.InvalidCredentials)
        );
    }
}
