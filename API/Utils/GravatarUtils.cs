using System.Security.Cryptography;
using System.Text;

namespace ShockLink.API.Utils;

public static class GravatarUtils
{
    public static Uri GetImageUrl(string email)
    {
        Span<byte> tempSpan = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(email), tempSpan);
        return new Uri($"https://www.gravatar.com/avatar/{Convert.ToHexString(tempSpan).ToLowerInvariant()}?d=https%3A%2F%2Fstatic.wikia.nocookie.net%2Frickandmorty%2Fimages%2Fe%2Fee%2FMorty501.png");
    }
    
}