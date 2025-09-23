using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Requests;
using OpenShock.Common.Errors;
using OpenShock.Common.Utils;

namespace OpenShock.API.Controller.Account.Authenticated;

public sealed partial class AuthenticatedAccountController
{
    /// <summary>
    /// Change the password of the current user
    /// </summary>
    /// <param name="body"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    [HttpPost("password")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest body)
    {
        if (!string.IsNullOrEmpty(CurrentUser.PasswordHash) && !HashingUtils.VerifyPassword(body.CurrentPassword, CurrentUser.PasswordHash).Verified)
        {
            return Problem(AccountError.PasswordChangeInvalidPassword);
        }
        
        var result = await _accountService.ChangePasswordAsync(CurrentUser.Id, body.NewPassword);

        return result.Match<IActionResult>(
            success => Ok(),
            deactivated => Problem(AccountError.AccountDeactivated),
            notFound => throw new Exception("Unexpected result, apparently our current user does not exist...")
            );
    }
}