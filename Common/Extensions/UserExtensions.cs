using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;

namespace OpenShock.Common.Extensions;

public static class UserExtensions
{
    public static Uri GetImageLink(this User user) => GravatarUtils.GetImageUrl(user.Email);

    public static bool IsUser(this User user, Guid otherUserId)
    {
        return user.Id == otherUserId;
    }

    public static bool IsUser(this User user, User otherUser)
    {
        return user == otherUser || user.Id == otherUser.Id;
    }

    public static bool IsRank(this User user, RankType rank)
    {
        return user.Rank >= rank;
    }

    public static bool IsUserOrRank(this User user, User otherUser, RankType rank)
    {
        if (user.IsUser(otherUser)) return true;

        return user.Rank >= rank;
    }

    public static bool IsUserOrRank(this User user, Guid otherUserId, RankType rank)
    {
        if (user.IsUser(otherUserId)) return true;

        return user.Rank >= rank;
    }
}