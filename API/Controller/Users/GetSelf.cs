using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.ServicesCommon.Authentication;

namespace OpenShock.API.Controller.Users;

public sealed partial class UsersController : AuthenticatedSessionControllerBase
{
    /// <summary>
    /// Gets the current user's information.
    /// </summary>
    /// <response code="200">The user's information was successfully retrieved.</response>
    [HttpGet("self", Name = "UsersGetSelf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public BaseResponse<SelfResponse> GetSelf() => new()
    {
        Data = new SelfResponse
        {
            Id = CurrentUser.DbUser.Id,
            Name = CurrentUser.DbUser.Name,
            Email = CurrentUser.DbUser.Email,
            Image = CurrentUser.GetImageLink(),
            Rank = CurrentUser.DbUser.Rank
        }
    };
    public class SelfResponse
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required Uri Image { get; set; }
        public required RankType Rank { get; set; }
    }
}