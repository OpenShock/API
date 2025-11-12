using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Requests;
using System.Net;
using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.API.Errors;
using OpenShock.API.Services.Turnstile;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using OpenShock.Common.Utils;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Signs up a new user
    /// </summary>
    /// <param name="body"></param>
    /// <param name="turnstileService"></param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">User successfully signed up</response>
    /// <response code="400">Username or email already exists</response>
    [HttpPost("signup")]
    [EnableRateLimiting("auth")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)] // EmailOrUsernameAlreadyExists
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)] // InvalidTurnstileResponse
    [MapToApiVersion("2")]
    [EndpointGroupName("v2")]
    public async Task<IActionResult> SignUpV2(
        [FromBody] SignUpV2 body,
        [FromServices] ICloudflareTurnstileService turnstileService,
        CancellationToken cancellationToken)
    {
        var turnStile = await turnstileService.VerifyUserResponseTokenAsync(body.TurnstileResponse, HttpContext.GetRemoteIP(), cancellationToken);
        if (!turnStile.IsT0)
        {
            var cfErrors = turnStile.AsT1.Value;
            if (cfErrors.All(err => err == CloudflareTurnstileError.InvalidResponse))
                return Problem(TurnstileError.InvalidTurnstile);

            return Problem(new OpenShockProblem("InternalServerError", "Internal Server Error", HttpStatusCode.InternalServerError));
        }

        var creationAction = await _accountService.CreateAccountWithActivationFlowAsync(body.Email, body.Username, body.Password);
        return creationAction.Match<IActionResult>(
            _ => Ok(),
            _ => Problem(SignupError.UsernameOrEmailExists)
        );
    }
}