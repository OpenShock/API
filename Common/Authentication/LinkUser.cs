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

    public bool IsRank(RankType rank)
    {
        return DbUser.Rank >= rank;
    }

    public bool IsUserOrRank(User user, RankType rank)
    {
        if (IsUser(user)) return true;

        return DbUser.Rank >= rank;
    }

    public bool IsUserOrRank(Guid userId, RankType rank)
    {
        if (IsUser(userId)) return true;

        return DbUser.Rank >= rank;
    }
}