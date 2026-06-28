using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Requests;
using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Services.Turnstile;
using OpenShock.Common.Errors;
using OpenShock.Common.Options;
using OpenShock.Common.Problems;

using OpenShock.Internal.Common.Problems;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Signs up a new user
    /// </summary>
    /// <param name="body"></param>
    /// <param name="turnstileService"></param>
    /// <param name="accountOptions"></param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">User successfully signed up</response>
    /// <response code="400">Username or email already exists</response>
    [HttpPost("signup")]
    [EnableRateLimiting("auth")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)] // EmailOrUsernameAlreadyExists
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)] // RegistrationDisabled or InvalidTurnstileResponse
    [MapToApiVersion("2")]
    public async Task<IActionResult> SignUpV2(
        [FromBody] SignUpV2 body,
        [FromServices] ICloudflareTurnstileService turnstileService,
        [FromServices] AccountOptions accountOptions,
        CancellationToken cancellationToken)
    {
        if (!accountOptions.RegistrationEnabled)
            return Problem(SignupError.RegistrationDisabled);

        var turnstileError = await VerifyTurnstileAsync(turnstileService, body.TurnstileResponse, cancellationToken);
        if (turnstileError is not null) return turnstileError;

        var creationAction = await _accountService.CreateAccountWithActivationFlowAsync(body.Email, body.Username, body.Password);
        return creationAction.Match<IActionResult>(
            _ => Ok(),
            _ => Problem(SignupError.UsernameOrEmailExists)
        );
    }
}