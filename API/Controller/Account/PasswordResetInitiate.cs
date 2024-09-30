using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Services.Account;
using OpenShock.Common.Models;
using OpenShock.ServicesCommon.Problems;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Initiate a password reset
    /// </summary>
    /// <response code="200">Password reset email sent if the email is associated to an registered account</response>
    [HttpPost("reset")]
    [ProducesSuccess]
    [MapToApiVersion("1")]
    public async Task<BaseResponse<object>> PasswordResetInitiate([FromBody] ResetRequest body, [FromServices] IAccountService accountService)
    {
        await accountService.CreatePasswordReset(body.Email);
        return SendResponse();
    }

    private static BaseResponse<object> SendResponse() => new("Password reset has been sent via email if the email is associated to an registered account");

    public sealed class ResetRequest
    {
        public required string Email { get; init; }
    }
}