﻿using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Requests;
using System.Net;
using System.Net.Mime;
using Asp.Versioning;
using OpenShock.API.Services.Account;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using OpenShock.Common.Services.Turnstile;
using OpenShock.Common.Utils;
using OpenShock.Common.Models;
using Microsoft.Extensions.Options;
using OpenShock.Common.Options;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Authenticate a user
    /// </summary>
    /// <response code="200">User successfully logged in</response>
    /// <response code="401">Invalid username or password</response>
    [HttpPost("login")]
    [ProducesResponseType<LegacyEmptyResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)] // InvalidCredentials
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)] // InvalidDomain
    [MapToApiVersion("2")]
    public async Task<IActionResult> LoginV2(
        [FromBody] LoginV2 body,
        [FromServices] ICloudflareTurnstileService turnstileService,
        [FromServices] IOptions<FrontendOptions> options,
        CancellationToken cancellationToken)
    {
        var cookieDomainToUse = options.Value.CookieDomain.Split(',').FirstOrDefault(domain => Request.Headers.Host.ToString().EndsWith(domain, StringComparison.OrdinalIgnoreCase));
        if (cookieDomainToUse is null) return Problem(LoginError.InvalidDomain);

        var remoteIP = HttpContext.GetRemoteIP();

        var turnStile = await turnstileService.VerifyUserResponseTokenAsync(body.TurnstileResponse, remoteIP, cancellationToken);
        if (!turnStile.IsT0)
        {
            var cfErrors = turnStile.AsT1.Value;
            if (cfErrors.All(err => err == CloduflareTurnstileError.InvalidResponse))
                return Problem(TurnstileError.InvalidTurnstile);

            return Problem(new OpenShockProblem("InternalServerError", "Internal Server Error", HttpStatusCode.InternalServerError));
        }
            
        var loginAction = await _accountService.CreateUserLoginSessionAsync(body.UsernameOrEmail, body.Password, new LoginContext
        {
            Ip = remoteIP.ToString(),
            UserAgent = HttpContext.GetUserAgent(),
        }, cancellationToken);

        return loginAction.Match<IActionResult>(
            ok =>
            {
                HttpContext.SetSessionKeyCookie(loginAction.AsT0.Value, "." + cookieDomainToUse);
                return Ok("Successfully logged in");
            },
            notActivated => Problem(LoginError.AccountNotActivated),
            deactivated => Problem(LoginError.AccountDeactivated),
            notFound => Problem(LoginError.InvalidCredentials)
        );
    }
}