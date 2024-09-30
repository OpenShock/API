using OpenShock.Common.OpenShockDb;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace OpenShock.ServicesCommon.Utils;

public static class GravatarUtils
{
    private static readonly string DefaultImageUrl = HttpUtility.UrlEncode("https://openshock.app/static/images/Icon512.png");

    public static Uri GetImageUrl(string email)
    {
        Span<byte> tempSpan = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(email), tempSpan);
        return new Uri($"https://www.gravatar.com/avatar/{Convert.ToHexString(tempSpan).ToLowerInvariant()}?d={DefaultImageUrl}");
    }

    public static Uri GetImage(this User user) => GetImageUrl(user.Email);
}