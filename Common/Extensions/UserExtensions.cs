using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;

namespace OpenShock.Common.Extensions;

public static class UserExtensions
{
    public static Uri GetImageUrl(this User user) => GravatarUtils.GetUserImageUrl(user.Email);

    public static bool IsUser(this User user, Guid otherUserId)
    {
        return user.Id == otherUserId;
    }

    public static bool IsUser(this User user, User otherUser)
    {
        return user == otherUser || user.Id == otherUser.Id;
    }

    public static bool IsRole(this User user, RoleType role)
    {
        return user.Roles.Contains(role);
    }

    public static bool IsUserOrRole(this User user, Guid otherUserId, RoleType role)
    {
        return user.IsUser(otherUserId) || user.IsRole(role);
    }

    public static bool IsUserOrRole(this User user, User otherUser, RoleType role)
    {
        return user.IsUser(otherUser) || user.IsRole(role);
    }
}
