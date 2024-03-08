using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Models;
using System.Net;
using Asp.Versioning;
using OpenShock.API.Services.Account;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Check if a password reset is in progress
    /// </summary>
    /// <param name="passwordResetId">The id of the password reset</param>
    /// <param name="secret">The secret of the password reset</param>
    /// <param name="accountService"></param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">Valid password reset process</response>
    /// <response code="404">Password reset process not found</response>
    [HttpHead("recover/{passwordResetId}/{secret}")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [MapToApiVersion("1")]
    public async Task<BaseResponse<object>> PasswordResetCheckValid([FromRoute] Guid passwordResetId, [FromRoute] string secret, [FromServices] IAccountService accountService, CancellationToken cancellationToken)
    {
        var passwordResetExists = await accountService.PasswordResetExists(passwordResetId, secret, cancellationToken);
        return passwordResetExists.Match(
            success => new BaseResponse<object>(),
            notFound => NotFoundPasswordReset(),
            invalid => NotFoundPasswordReset()
        );
    }
}