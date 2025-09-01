using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Models.Response;

public sealed class LoginV2OkResponse
{
    public required Guid AccountId { get; init; }
    public required string AccountName { get; init; }
    public required string AccountEmail { get; init; }
    public required Uri ProfileImage { get; init; }
    public required List<RoleType> AccountRoles { get; init; }

    public static LoginV2OkResponse FromUser(User argUser) => new()
    {
        AccountId = argUser.Id,
        AccountName = argUser.Name,
        AccountEmail = argUser.Email,
        ProfileImage = argUser.GetImageUrl(),
        AccountRoles = argUser.Roles
    };
}