using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Response;
using OpenShock.Common.Extensions;
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
    [ProducesResponseType<LegacyDataResponse<LegacyUserSelfResponse>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public IActionResult GetSelf()
    {
        return LegacyDataOk(
            new LegacyUserSelfResponse
            {
                Id = CurrentUser.Id,
                Name = CurrentUser.Name,
                Email = CurrentUser.Email,
                Image = CurrentUser.GetImageUrl(),
                Roles = CurrentUser.Roles,
                Rank = CurrentUser.Roles.Count > 0 ? CurrentUser.Roles.Max().ToString() : "User",
                HasPassword = !string.IsNullOrEmpty(CurrentUser.PasswordHash)
            }
        );
    }
}
