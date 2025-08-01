using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Requests;
using System.Net.Mime;
using Asp.Versioning;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using OpenShock.Common.Models;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Signs up a new user
    /// </summary>
    /// <param name="body"></param>
    /// <response code="200">User successfully signed up</response>
    /// <response code="409">Username or email already exists</response>
    [HttpPost("signup")]
    [ProducesResponseType<LegacyEmptyResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)] // EmailOrUsernameAlreadyExists
    [MapToApiVersion("1")]
    public async Task<IActionResult> SignUp([FromBody] SignUp body)
    {
        var creationAction = await _accountService.CreateUnverifiedAccountLegacyAsync(body.Email, body.Username, body.Password);
        if (creationAction.IsT1) return Problem(SignupError.EmailAlreadyExists);

        return LegacyEmptyOk("Successfully signed up");
    }
}