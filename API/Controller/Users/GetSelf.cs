using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;

namespace OpenShock.API.Controller.Users;

public sealed partial class UsersController
{
    /// <summary>
    /// Get the current user's information.
    /// </summary>
    /// <response code="200">The user's information was successfully retrieved.</response>
    [HttpGet("self")]
    [ProducesResponseType<LegacyDataResponse<UserSelfResponse>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public IActionResult GetSelf()
    {
        return LegacyDataOk(
            new UserSelfResponse
            {
                Id = CurrentUser.Id,
                Name = CurrentUser.Name,
                Email = CurrentUser.Email,
                Image = CurrentUser.GetImageUrl(),
                Roles = CurrentUser.Roles,
                Rank = CurrentUser.Roles.Count > 0 ? CurrentUser.Roles.Max().ToString() : "User"
            }
        );
    }

    public sealed class UserSelfResponse
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public required string Email { get; init; }
        public required Uri Image { get; init; }
        public required List<RoleType> Roles { get; init; }
        public required string Rank { get; init; }
    }
}