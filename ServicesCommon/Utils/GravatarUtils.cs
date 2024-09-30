using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;
using System.Web;

namespace OpenShock.ServicesCommon.Utils;

public static class GravatarUtils
{
    private static readonly string DefaultImageUrl = HttpUtility.UrlEncode("https://openshock.app/static/images/Icon512.png");

    public static Uri GetImageUrl(string email) => new Uri($"https://www.gravatar.com/avatar/{HashingUtils.HashSha256(email).ToLowerInvariant()}?d={DefaultImageUrl}");

    public static Uri GetImage(this User user) => GetImageUrl(user.Email);
}