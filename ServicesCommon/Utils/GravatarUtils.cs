using System.Security.Cryptography;
using System.Text;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.ServicesCommon.Utils;

public static class GravatarUtils
{
    public static Uri GetImageUrl(string email)
    {
        Span<byte> tempSpan = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(email), tempSpan);
        return new Uri($"https://www.gravatar.com/avatar/{Convert.ToHexString(tempSpan).ToLowerInvariant()}?d=https%3A%2F%2Fshocklink.net%2Fstatic%2Fimages%2FIcon512.png");
    }

    public static Uri GetImage(this User user) => GetImageUrl(user.Email);
}
