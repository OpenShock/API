using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Requests;
using OpenShock.API.Models.Response;
using OpenShock.ServicesCommon.Errors;
using OpenShock.ServicesCommon.Problems;

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
    [ProducesSuccess]
    public async Task<IActionResult> ChangeUsername(ChangeUsernameRequest data)
    {
        var result = await _accountService.ChangeUsername(CurrentUser.DbUser.Id, data.Username);

        return result.Match(success => RespondSuccessSimple(),
            error => Problem(error.Value == UsernameCheckResult.Taken
                ? AccountError.UsernameTaken
                : AccountError.UsernameUnavailable),
            found => throw new Exception("Unexpected result, apparently our current user does not exist..."));
    }
}