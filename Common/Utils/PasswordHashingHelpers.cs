using BCrypt.Net;

namespace OpenShock.Common.Utils;

public static class PasswordHashingHelpers
{
    private const string BCryptPrefix = "bcrypt:";
    private const string PBKDF2Prefix = "pbkdf2:";

    private const HashType HashAlgo = HashType.SHA512;

    public struct VerifyPasswordResult
    {
        public bool Verified;
        public bool NeedsRehash;
    }

    public static VerifyPasswordResult VerifyPassword(string password, string combinedHash)
    {
        if (combinedHash.StartsWith(BCryptPrefix))
        {
            return new VerifyPasswordResult
            {
                Verified = BCrypt.Net.BCrypt.EnhancedVerify(password, combinedHash[BCryptPrefix.Length..], HashAlgo),
                NeedsRehash = false
            };
        }

        if (combinedHash.StartsWith(PBKDF2Prefix))
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return new VerifyPasswordResult
            {
                Verified = SecurePasswordHasher.Verify(password, combinedHash[PBKDF2Prefix.Length..]),
                NeedsRehash = true
            };
#pragma warning restore CS0618 // Type or member is obsolete
        }

        return new VerifyPasswordResult
        {
            Verified = false,
            NeedsRehash = false
        };
    }

    public static string HashPassword(string password)
    {
        return BCryptPrefix + BCrypt.Net.BCrypt.EnhancedHashPassword(password, HashAlgo);
    }
}
