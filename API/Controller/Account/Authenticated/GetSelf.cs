using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Response;
using OpenShock.Common.Authentication;
using OpenShock.Common.Extensions;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Account.Authenticated;

public sealed partial class AuthenticatedAccountController
{
    /// <summary>
    /// Get the current user's account information.
    /// </summary>
    /// <response code="200">The account information was successfully retrieved.</response>
    [HttpGet]
    [Authorize(AuthenticationSchemes = OpenShockAuthSchemes.UserSessionApiTokenCombo)]
    [ProducesResponseType<UserSelfResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public UserSelfResponse GetSelf() => CreateSelfResponse(CurrentUser);

    internal static UserSelfResponse CreateSelfResponse(OpenShock.Common.OpenShockDb.User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email,
        Image = user.GetImageUrl(),
        Roles = user.Roles,
        Rank = user.Roles.Count > 0 ? user.Roles.Max().ToString() : "User",
        HasPassword = !string.IsNullOrEmpty(user.PasswordHash)
    };
}
