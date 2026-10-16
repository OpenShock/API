using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Controller.Account.Authenticated;
using OpenShock.API.Models.Response;
using OpenShock.Common.Models;

namespace OpenShock.API.Controller.Users;

public sealed partial class UsersController
{
    /// <summary>
    /// Get the current user's information. Deprecated: use GET /1/account instead.
    /// </summary>
    /// <response code="200">The user's information was successfully retrieved.</response>
    [Obsolete("Use GET /1/account instead.")]
    [HttpGet("self")]
    [ProducesResponseType<LegacyDataResponse<UserSelfResponse>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public IActionResult GetSelf()
    {
        return LegacyDataOk(AuthenticatedAccountController.CreateSelfResponse(CurrentUser));
    }
}
