using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Requests;
using OpenShock.API.Models.Response;
using OpenShock.ServicesCommon.Problems;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Check if a username is available
    /// </summary>
    /// <param name="data"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("username/check")]
    [ProducesSuccess<UsernameCheckResult>]
    public async Task<IActionResult> CheckUsername(ChangeUsernameRequest data, CancellationToken cancellationToken)
    {
        var availability = await _accountService.CheckUsernameAvailability(data.Username, cancellationToken);
        return RespondSuccess(availability);
    }
}