using BCrypt.Net;
using OpenShock.Common.Models;

namespace OpenShock.Common.Utils;

public static class PasswordHashingUtils
{
    private const string BCryptPrefix = "bcrypt";
    private const string PBKDF2Prefix = "pbkdf2";

    private const HashType BCryptHashType = HashType.SHA512;

    public readonly record struct VerifyPasswordResult(bool Verified, bool NeedsRehash);

    private static readonly VerifyPasswordResult VerifyPasswordFailureResult = new(false, false);
    
    private static PasswordHashingAlgorithm PasswordHashingAlgorithmFromPrefix(ReadOnlySpan<char> prefix)
    {
        return prefix switch
        {
            BCryptPrefix => PasswordHashingAlgorithm.BCrypt,
            PBKDF2Prefix => PasswordHashingAlgorithm.PBKDF2,
            _ => PasswordHashingAlgorithm.Unknown,
        };
    }

    public static PasswordHashingAlgorithm GetPasswordHashingAlgorithm(ReadOnlySpan<char> combinedHash)
    {
        int index = combinedHash.IndexOf(':');
        if (index < 0) return PasswordHashingAlgorithm.Unknown;
        
        return PasswordHashingAlgorithmFromPrefix(combinedHash[..index]);
    }

    public static VerifyPasswordResult VerifyPassword(string password, string combinedHash)
    {
        int index = combinedHash.IndexOf(':');
        if (index < 0) return VerifyPasswordFailureResult;
        
        var algorithm = PasswordHashingAlgorithmFromPrefix(combinedHash.AsSpan(0, index));

        if (algorithm == PasswordHashingAlgorithm.BCrypt)
        {
            return new VerifyPasswordResult
            {
                Verified = BCrypt.Net.BCrypt.EnhancedVerify(password, combinedHash[(index + 1)..], BCryptHashType),
                NeedsRehash = false
            };
        }

        if (algorithm == PasswordHashingAlgorithm.PBKDF2)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return new VerifyPasswordResult
            {
                Verified = PBKDF2PasswordHasher.Verify(password, combinedHash, customName: PBKDF2Prefix + ":"),
                NeedsRehash = true
            };
#pragma warning restore CS0618 // Type or member is obsolete
        }
        
        return VerifyPasswordFailureResult;
    }

    public static string HashPassword(string password)
    {
        return $"{BCryptPrefix}:{BCrypt.Net.BCrypt.EnhancedHashPassword(password, BCryptHashType)}";
    }
}
