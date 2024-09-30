using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Requests;
using OpenShock.API.Services.Account;
using OpenShock.ServicesCommon.Errors;
using OpenShock.ServicesCommon.Problems;
using System.Net;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Signs up a new user
    /// </summary>
    /// <param name="body"></param>
    /// <param name="accountService"></param>
    /// <response code="200">User successfully signed up</response>
    /// <response code="409">Username or email already exists</response>
    [HttpPost("signup")]
    [ProducesSuccess]
    [ProducesProblem(HttpStatusCode.Conflict, "EmailOrUsernameAlreadyExists")]
    [MapToApiVersion("1")]
    public async Task<IActionResult> SignUp([FromBody] SignUp body,
        [FromServices] IAccountService accountService)
    {
        var creationAction = await accountService.CreateAccount(body.Email, body.Username, body.Password);
        if (creationAction.IsT1) return Problem(SignupError.EmailAlreadyExists);


        return RespondSuccessSimple("Successfully signed up");
    }
}