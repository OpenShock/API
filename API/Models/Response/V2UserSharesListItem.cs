using OpenShock.Common.Models;
using OpenShock.Common.Models.WebSocket.User;
using OpenShock.Common.Utils;

namespace OpenShock.API.Models.Response;

public class V2UserSharesListItem : GenericIni
{
    public required IEnumerable<UserShareInfo> Shares { get; init; }
}

public class V2UserSharesListItemDto : GenericIn
{
    public required string Email { get; init; }
    public required IEnumerable<UserShareInfo> Shares { get; init; }
}

public static class V2UserSharesListItemExtensions
{
    public static V2UserSharesListItem FromDto(this V2UserSharesListItemDto item) =>
        new()
        {
            Id = item.Id,
            Name = item.Name,
            Image = GravatarUtils.GetUserImageUrl(item.Email),
            Shares = item.Shares
        };
}