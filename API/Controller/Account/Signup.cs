using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Requests;
using OpenShock.Common.Models;
using System.Net;
using Asp.Versioning;
using OpenShock.API.Services.Account;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Signs up a new user
    /// </summary>
    /// <param name="body"></param>
    /// <param name="accountService"></param>
    /// <response code="200">User successfully signed up</response>
    /// <response code="400">Username or email already exists</response>
    [HttpPost("signup")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [MapToApiVersion("1")]
    public async Task<BaseResponse<object>> SignUp([FromBody] SignUp body,
        [FromServices] IAccountService accountService)
    {
        var creationAction = await accountService.CreateAccount(body.Email, body.Username, body.Password);
        if (creationAction.IsT1)
            return EBaseResponse<object>(
                "Account with same username or email already exists. Please choose a different username or reset your password.");


        return new BaseResponse<object>("Successfully created account");
    }
}