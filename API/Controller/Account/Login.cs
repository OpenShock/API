using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenShock.API.Models.Requests;
using OpenShock.API.Services.Account;
using OpenShock.Common.Errors;
using OpenShock.Common.Models;
using OpenShock.Common.Options;
using OpenShock.Common.Problems;
using OpenShock.Common.Utils;
using System.Net.Mime;
using Microsoft.AspNetCore.RateLimiting;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Authenticate a user
    /// </summary>
    /// <response code="200">User successfully logged in</response>
    /// <response code="401">Invalid username or password</response>
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<LegacyEmptyResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)] // InvalidCredentials
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)] // InvalidDomain
    [MapToApiVersion("1")]
    public async Task<IActionResult> Login(
        [FromBody] Login body,
        [FromServices] IOptions<FrontendOptions> options,
        CancellationToken cancellationToken)
    {
        var cookieDomainToUse = options.Value.CookieDomain.Split(',').FirstOrDefault(domain => Request.Headers.Host.ToString().EndsWith(domain, StringComparison.OrdinalIgnoreCase));
        if (cookieDomainToUse is null) return Problem(LoginError.InvalidDomain);

        var loginAction = await _accountService.CreateUserLoginSessionAsync(body.Email, body.Password, new LoginContext
        {
            Ip = HttpContext.GetRemoteIP().ToString(),
            UserAgent = HttpContext.GetUserAgent(),
        }, cancellationToken);

        if (loginAction.IsT1) return Problem(LoginError.InvalidCredentials);

        HttpContext.SetSessionKeyCookie(loginAction.AsT0.Value, "." + cookieDomainToUse);

        return LegacyEmptyOk("Successfully logged in");
    }
}