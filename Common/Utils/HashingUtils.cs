using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using OpenShock.Common.Models;

namespace OpenShock.Common.Utils;

public static class HashingUtils
{
    private const string BCryptPrefix = "bcrypt";
    private const string Pbkdf2Prefix = "pbkdf2";
    private const HashType BCryptHashType = HashType.SHA512;
    
    public readonly record struct VerifyHashResult(bool Verified, bool NeedsRehash);
    private static readonly VerifyHashResult VerifyHashFailureResult = new(false, false);
    
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
    
    private static PasswordHashingAlgorithm PasswordHashingAlgorithmFromPrefix(ReadOnlySpan<char> prefix)
    {
        return prefix switch
        {
            BCryptPrefix => PasswordHashingAlgorithm.BCrypt,
            Pbkdf2Prefix => PasswordHashingAlgorithm.PBKDF2,
            _ => PasswordHashingAlgorithm.Unknown,
        };
    }
    public static PasswordHashingAlgorithm GetPasswordHashingAlgorithm(ReadOnlySpan<char> combinedHash)
    {
        int index = combinedHash.IndexOf(':');
        if (index <= 0) return PasswordHashingAlgorithm.Unknown;
        
        return PasswordHashingAlgorithmFromPrefix(combinedHash[..index]);
    }

    public static string HashPassword(string password)
    {
        return $"{BCryptPrefix}:{BCrypt.Net.BCrypt.EnhancedHashPassword(password, BCryptHashType)}";
    }
    public static VerifyHashResult VerifyPassword(string password, string combinedHash)
    {
        int index = combinedHash.IndexOf(':');
        if (index <= 0) return VerifyHashFailureResult;
        
        var algorithm = PasswordHashingAlgorithmFromPrefix(combinedHash.AsSpan(0, index));

        if (algorithm == PasswordHashingAlgorithm.BCrypt)
        {
            return new VerifyHashResult
            {
                Verified = BCrypt.Net.BCrypt.EnhancedVerify(password, combinedHash[(index + 1)..], BCryptHashType),
                NeedsRehash = false
            };
        }

        if (algorithm == PasswordHashingAlgorithm.PBKDF2)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return new VerifyHashResult
            {
                Verified = PBKDF2PasswordHasher.Verify(password, combinedHash, customName: Pbkdf2Prefix + ":"),
                NeedsRehash = true
            };
#pragma warning restore CS0618 // Type or member is obsolete
        }
        
        return VerifyHashFailureResult;
    }

    public static string HashToken(string token)
    {
        return HashSha256(token);
    }
    public static VerifyHashResult VerifyToken(string token, string hashedToken)
    {
        if (string.IsNullOrEmpty(token)) return VerifyHashFailureResult;

        bool isOldHashType = hashedToken[0] == '$';
        if (isOldHashType)
        {
            bool matches = BCrypt.Net.BCrypt.EnhancedVerify(token, hashedToken, BCryptHashType);
            return new VerifyHashResult(matches, true);
        }
        
        return new VerifyHashResult(HashToken(token) == hashedToken, false);
    }
}
