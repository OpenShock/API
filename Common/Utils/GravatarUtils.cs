using OpenShock.Common.OpenShockDb;
using System.Web;

namespace OpenShock.Common.Utils;

public static class GravatarUtils
{
    private static readonly string DefaultImageUrl = HttpUtility.UrlEncode("https://openshock.app/static/images/Icon512.png");

    public static Uri GetImageUrl(string email) => new Uri($"https://www.gravatar.com/avatar/{HashingUtils.HashSha256(email)}?d={DefaultImageUrl}");

    public static Uri GetImage(this User user) => GetImageUrl(user.Email);
}