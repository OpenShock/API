using System.Security.Cryptography;
using System.Text;

namespace ShockLink.API.Utils;

public static class GravatarUtils
{
    public static Uri GetImageUrl(string email)
    {
        Span<byte> tempSpan = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(email), tempSpan);
        return new Uri($"https://www.gravatar.com/avatar/{Convert.ToHexString(tempSpan)}/");
    }
    
}