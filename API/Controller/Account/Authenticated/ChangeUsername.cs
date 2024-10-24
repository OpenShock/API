﻿using System.Net;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Requests;
using OpenShock.Common.Errors;
using OpenShock.Common.Models;
using OpenShock.Common.Problems;

namespace OpenShock.API.Controller.Account.Authenticated;

public sealed partial class AuthenticatedAccountController
{
    /// <summary>
    /// Change the username of the current user
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    [HttpPost("username")]
    [ProducesSlimSuccess]
    [ProducesProblem(HttpStatusCode.Conflict, "UsernameTaken")]
    [ProducesProblem(HttpStatusCode.BadRequest, "UsernameInvalid")]
    [ProducesProblem(HttpStatusCode.Forbidden, "UsernameRecentlyChanged")]
    public async Task<IActionResult> ChangeUsername(ChangeUsernameRequest data)
    {
        var result = await _accountService.ChangeUsername(CurrentUser.DbUser.Id, data.Username,
            CurrentUser.DbUser.Rank.IsAllowed(RankType.Staff));

        return result.Match(
            success => RespondSlimSuccess(),
            error => Problem(error.Value.Match(
                taken => AccountError.UsernameTaken,
                AccountError.UsernameInvalid,
                changed => AccountError.UsernameRecentlyChanged)),
            found => throw new Exception("Unexpected result, apparently our current user does not exist..."));
    }
}