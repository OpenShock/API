using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Requests;
using System.Net;
using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using OpenShock.Common.Utils;
using OpenShock.API.Errors;
using OpenShock.API.Models.Response;
using OpenShock.API.Services.Turnstile;

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
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<LoginV2OkResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)] // InvalidCredentials
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)] // InvalidDomain
    [MapToApiVersion("2")]
    [EndpointGroupName("v2")]
    public async Task<IActionResult> LoginV2(
        [FromBody] LoginV2 body,
        [FromServices] ICloudflareTurnstileService turnstileService,
        CancellationToken cancellationToken)
    {
        var cookieDomain = GetCurrentCookieDomain();
        if (cookieDomain is null) return Problem(LoginError.InvalidDomain);

        var remoteIp = HttpContext.GetRemoteIP();

        var turnStile = await turnstileService.VerifyUserResponseTokenAsync(body.TurnstileResponse, remoteIp, cancellationToken);
        if (!turnStile.TryPickT0(out _, out var cfErrors))
        {
            if (cfErrors.Value.All(err => err == CloudflareTurnstileError.InvalidResponse))
                return Problem(TurnstileError.InvalidTurnstile);

            return Problem(new OpenShockProblem("InternalServerError", "Internal Server Error", HttpStatusCode.InternalServerError));
        }
        
        var getAccountResult = await _accountService.GetAccountByCredentialsAsync(body.UsernameOrEmail, body.Password, cancellationToken);
        if (!getAccountResult.TryPickT0(out var account, out var errors))
        {
            return errors.Match(
                notFound => Problem(LoginError.InvalidCredentials),
                deactivated => Problem(AccountError.AccountDeactivated),
                notActivated => Problem(AccountError.AccountNotActivated),
                oauthOnly => Problem(AccountError.AccountOAuthOnly)
            );
        }
        
        await CreateSession(account.Id, cookieDomain);
        
        return Ok(LoginV2OkResponse.FromUser(account));
    }
}