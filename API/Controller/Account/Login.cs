using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Requests;
using OpenShock.Common.Errors;
using OpenShock.Common.Models;
using OpenShock.Common.Problems;
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
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<LegacyEmptyResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)] // InvalidCredentials
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)] // InvalidDomain
    [MapToApiVersion("1")]
    [EndpointGroupName("v1")]
    public async Task<IActionResult> Login(
        [FromBody] Login body,
        CancellationToken cancellationToken)
    {
        var cookieDomain = GetCurrentCookieDomain();
        if (cookieDomain is null) return Problem(LoginError.InvalidDomain);

        var getAccountResult = await _accountService.GetAccountByCredentialsAsync(body.Email, body.Password, cancellationToken);
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
        return LegacyEmptyOk("Successfully logged in");
    }
}