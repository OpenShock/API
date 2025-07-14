using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Requests;
using OpenShock.Common.Models;

namespace OpenShock.API.Controller.Account.Authenticated;

public sealed partial class AuthenticatedAccountController
{
    /// <summary>
    /// Change the password of the current user
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    [HttpPost("email")]
    [ProducesResponseType<LegacyEmptyResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public Task<IActionResult> ChangeEmail(ChangeEmailRequest data)
    {
        throw new NotImplementedException();
    }
}