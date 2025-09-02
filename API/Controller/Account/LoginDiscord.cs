using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using OpenShock.API.Models.Requests;
using OpenShock.API.Models.Response;
using OpenShock.API.Services.Account;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using OpenShock.Common.Options;
using OpenShock.Common.Utils;
using OpenShock.Common.Models;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    [HttpPost("login/discord")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<LoginV2OkResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)]
    [MapToApiVersion("2")]
    public async Task<IActionResult> LoginDiscord(
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
