using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;

namespace OpenShock.Common.Authentication;

public sealed class LinkUser
{
    public required User DbUser { get; set; }

    public Uri GetImageLink() => GravatarUtils.GetImageUrl(DbUser.Email);

    public bool IsUser(Guid userId)
    {
        return DbUser.Id == userId;
    }

    public bool IsUser(User user)
    {
        return DbUser == user || DbUser.Id == user.Id;
    }

    public bool IsUserOrRank(User user, RankType requiredRank)
    {
        if (IsUser(user)) return true;

        return DbUser.Rank >= requiredRank;
    }

    public bool IsUserOrRank(Guid userId, RankType requiredRank)
    {
        if (IsUser(userId)) return true;

        return DbUser.Rank >= requiredRank;
    }
}