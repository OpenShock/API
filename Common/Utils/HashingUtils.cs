using System.Buffers;
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
        Span<byte> hashDigest = stackalloc byte[SHA256.HashSizeInBytes];

        const int maxStackSize = 256; // Threshold for stack allocation
        int byteCount = Encoding.UTF8.GetByteCount(str);

        if (byteCount > maxStackSize)
        {
            byte[] decodedBytes = ArrayPool<byte>.Shared.Rent(byteCount);

            try
            {
                int decodedCount = Encoding.UTF8.GetBytes(str, decodedBytes);
                SHA256.HashData(decodedBytes.AsSpan(0, decodedCount), hashDigest);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(decodedBytes, true);
            }
        }
        else
        {
            Span<byte> decodedBytes = stackalloc byte[maxStackSize];
            int decodedCount = Encoding.UTF8.GetBytes(str, decodedBytes);
            SHA256.HashData(decodedBytes[..decodedCount], hashDigest);
        }


        return Convert.ToHexStringLower(hashDigest);
    }
}
