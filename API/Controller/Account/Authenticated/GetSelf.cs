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
    public UserSelfResponse GetSelf() => new()
    {
        Id = CurrentUser.Id,
        Name = CurrentUser.Name,
        Email = CurrentUser.Email,
        Image = CurrentUser.GetImageUrl(),
        Roles = CurrentUser.Roles,
        HasPassword = !string.IsNullOrEmpty(CurrentUser.PasswordHash)
    };
}
