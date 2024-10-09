using System.Security.Cryptography;
using System.Text;

namespace OpenShock.Common.Utils;

public static class HashingUtils
{
    /// <summary>
    /// Hashes string using SHA-256 and returns the result as a uppercase string
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string HashSha256(string str)
    {
        Span<byte> tempSpan = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(str), tempSpan);
        return Convert.ToHexString(tempSpan);
    }
}
