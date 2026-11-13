using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Errors;
using OpenShock.API.Models.Requests;
using OpenShock.API.Models.Response;
using OpenShock.API.Services.Account;
using OpenShock.API.Services.Turnstile;
using OpenShock.Common.Errors;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.AutomationTokens;
using OpenShock.Internal.Common.Problems;
using Results = OpenShock.Common.Results;

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
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)] // InvalidDomain, AutomationTokenNotAllowedForAccount
    [MapToApiVersion("2")]
    public async Task<IActionResult> LoginV2(
        [FromBody] LoginV2 body,
        [FromServices] ICloudflareTurnstileService turnstileService,
        [FromServices] IAutomationTokenService automationTokens,
        CancellationToken cancellationToken)
    {
        var cookieDomain = GetCurrentCookieDomain();
        if (cookieDomain is null) return Problem(LoginError.InvalidDomain);

        var turnstileError = await VerifyTurnstileAsync(turnstileService, body.TurnstileResponse, cancellationToken);
        if (turnstileError is not null) return turnstileError;

        // Checked before the password is: with Turnstile and rate limits lifted, an automation token must not
        // be usable to guess passwords of privileged accounts.
        if (!await automationTokens.CanUseForLoginAsync(body.UsernameOrEmail, cancellationToken))
            return Problem(AutomationTokenError.NotAllowedForAccount);

        var getAccountResult = await _accountService.GetAccountByCredentialsAsync(body.UsernameOrEmail, body.Password, cancellationToken);
        return getAccountResult switch
        {
            User account => await CompleteLoginAsync(account, cookieDomain),
            Results.NotFound => Problem(LoginError.InvalidCredentials),
            AccountDeactivated => Problem(AccountError.AccountDeactivated),
            AccountNotActivated => await ResendActivationAndRejectAsync(),
            AccountIsOAuthOnly => Problem(AccountError.AccountOAuthOnly)
        };

        async Task<IActionResult> CompleteLoginAsync(User account, string domain)
        {
            if (!await automationTokens.TryRecordUseAsync(account.Id, AutomationTokenFlow.Login, cancellationToken))
                return Problem(AutomationTokenError.NotAllowedForAccount);

            await CreateSession(account.Id, domain);

            return Ok(LoginV2OkResponse.FromUser(account));
        }

        // AccountNotActivated is only returned after the password checked out, so the caller owns this
        // account: (re)send the activation link. Rate limited inside the service, and the response is the
        // same whether or not a mail went out.
        async Task<IActionResult> ResendActivationAndRejectAsync()
        {
            try
            {
                await _accountService.ResendActivationEmailAsync(body.UsernameOrEmail, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Best effort: a failed resend must not turn the AccountNotActivated answer into a 500.
                _logger.LogError(ex, "Failed to resend activation email on login");
            }

            return Problem(AccountError.AccountNotActivated);
        }
    }
}
