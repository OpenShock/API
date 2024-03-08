using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Models;
using System.Net;
using Asp.Versioning;
using OpenShock.API.Services.Account;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Complete a password reset process
    /// </summary>
    /// <param name="passwordResetId">The id of the password reset</param>
    /// <param name="secret">The secret of the password reset</param>
    /// <param name="body"></param>
    /// <param name="accountService"></param>
    /// <response code="200">Password successfully changed</response>
    /// <response code="404">Password reset process not found</response>
    [HttpPost("recover/{passwordResetId}/{secret}")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [MapToApiVersion("1")]
    public async Task<BaseResponse<object>> PasswordResetComplete([FromRoute] Guid passwordResetId,
        [FromRoute] string secret, [FromBody] PasswordResetProcessData body,
        [FromServices] IAccountService accountService)
    {
        var passwordResetComplete = await accountService.PasswordResetComplete(passwordResetId, secret, body.Password);

        return passwordResetComplete.Match(
            success => new BaseResponse<object>("Successfully changed password"),
            notFound => NotFoundPasswordReset(),
            invalid => NotFoundPasswordReset());
    }

    private BaseResponse<object> NotFoundPasswordReset() =>
        EBaseResponse<object>("Password reset process not found", HttpStatusCode.NotFound);

    public sealed class PasswordResetProcessData
    {
        public required string Password { get; init; }
    }
}