﻿using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Requests;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using OpenShock.Common.Utils;

namespace OpenShock.API.Controller.Account.Authenticated;

public sealed partial class AuthenticatedAccountController
{
    /// <summary>
    /// Change the password of the current user
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    [HttpPost("password")]
    [ProducesSlimSuccess]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest data)
    {
        if (!PasswordHashingUtils.VerifyPassword(data.OldPassword, CurrentUser.DbUser.PasswordHash).Verified)
        {
            return Problem(AccountError.PasswordChangeInvalidPassword);
        }
        
        var result = await _accountService.ChangePassword(CurrentUser.DbUser.Id, data.NewPassword);

        return result.Match(success => RespondSlimSuccess(),
            notFound => throw new Exception("Unexpected result, apparently our current user does not exist..."));
    }
}