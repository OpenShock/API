using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Requests;
using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using OpenShock.Common.Errors;
using OpenShock.Common.Options;
using OpenShock.Common.Problems;
using OpenShock.Common.Models;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Signs up a new user
    /// </summary>
    /// <param name="body"></param>
    /// <param name="accountOptions"></param>
    /// <response code="200">User successfully signed up</response>
    /// <response code="409">Username or email already exists</response>
    /// <response code="403">Account registration is disabled on this instance</response>
    [HttpPost("signup")]
    [EnableRateLimiting("auth")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<LegacyEmptyResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)] // EmailOrUsernameAlreadyExists
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)] // RegistrationDisabled
    [MapToApiVersion("1")]
    public async Task<IActionResult> SignUp(
        [FromBody] SignUp body,
        [FromServices] AccountOptions accountOptions)
    {
        if (!accountOptions.RegistrationEnabled)
            return Problem(SignupError.RegistrationDisabled);

        var creationAction = await _accountService.CreateAccountWithoutActivationFlowLegacyAsync(body.Email, body.Username, body.Password);
        return creationAction.Match<IActionResult>(
            ok => LegacyEmptyOk("Successfully signed up"),
            alreadyExists => Problem(SignupError.UsernameOrEmailExists)
            );
    }
}