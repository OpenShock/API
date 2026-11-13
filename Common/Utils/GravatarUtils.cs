using System.Web;

namespace OpenShock.Common.Utils;

public static class GravatarUtils
{
    private static readonly string DefaultImageUrl = HttpUtility.UrlEncode("https://openshock.app/static/images/Icon512.png");

    private static Uri GetImageUrl(string id) => new($"https://www.gravatar.com/avatar/{id}?d={DefaultImageUrl}");

    public static readonly Uri GuestImageUrl = GetImageUrl("0");

    public static Uri GetUserImageUrl(string email) => GetImageUrl(HashingUtils.HashSha256(email));
}