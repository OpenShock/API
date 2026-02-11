using OpenShock.Common.Models;
using OpenShock.Common.Utils;

namespace OpenShock.Common.Tests.Utils;

public class HashingUtilsTests
{
    // --- HashSha256 ---

    [Test]
    [Arguments("test", "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08")]
    [Arguments("Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in", "2fac5f5f1d048a84fbb75c389f4596e05023ac17da4fcf45a5954d2d9a394301")]
    public async Task HashSha256(string str, string expectedHash)
    {
        var result = HashingUtils.HashSha256(str);
        await Assert.That(result).IsEqualTo(expectedHash);
    }

    [Test]
    public async Task HashSha256_EmptyString_ReturnsKnownHash()
    {
        // SHA-256 of empty string
        var result = HashingUtils.HashSha256("");
        await Assert.That(result).IsEqualTo("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
    }

    [Test]
    public async Task HashSha256_ReturnsLowercaseHex()
    {
        var result = HashingUtils.HashSha256("test");
        await Assert.That(result).IsEqualTo(result.ToLowerInvariant());
    }

    [Test]
    public async Task HashSha256_Returns64CharHex()
    {
        var result = HashingUtils.HashSha256("anything");
        await Assert.That(result.Length).IsEqualTo(64);
    }

    // --- HashPassword / VerifyPassword ---

    [Test]
    public async Task HashPassword_VerifyPassword_Roundtrip()
    {
        var password = "MySecureP@ssw0rd!";
        var hash = HashingUtils.HashPassword(password);

        var result = HashingUtils.VerifyPassword(password, hash);
        await Assert.That(result.Verified).IsTrue();
        await Assert.That(result.NeedsRehash).IsFalse();
    }

    [Test]
    public async Task HashPassword_StartsWithBcryptPrefix()
    {
        var hash = HashingUtils.HashPassword("test");
        await Assert.That(hash.StartsWith("bcrypt:")).IsTrue();
    }

    [Test]
    public async Task VerifyPassword_WrongPassword_ReturnsFalse()
    {
        var hash = HashingUtils.HashPassword("correct");
        var result = HashingUtils.VerifyPassword("wrong", hash);
        await Assert.That(result.Verified).IsFalse();
    }

    [Test]
    public async Task VerifyPassword_InvalidHashFormat_ReturnsFalse()
    {
        var result = HashingUtils.VerifyPassword("test", "notahash");
        await Assert.That(result.Verified).IsFalse();
    }

    [Test]
    public async Task VerifyPassword_EmptyHash_ReturnsFalse()
    {
        var result = HashingUtils.VerifyPassword("test", "");
        await Assert.That(result.Verified).IsFalse();
    }

    [Test]
    public async Task HashPassword_DifferentPasswords_DifferentHashes()
    {
        var hash1 = HashingUtils.HashPassword("password1");
        var hash2 = HashingUtils.HashPassword("password2");
        await Assert.That(hash1).IsNotEqualTo(hash2);
    }

    [Test]
    public async Task HashPassword_SamePassword_DifferentSalts()
    {
        var hash1 = HashingUtils.HashPassword("same");
        var hash2 = HashingUtils.HashPassword("same");
        await Assert.That(hash1).IsNotEqualTo(hash2);
    }

    // --- GetPasswordHashingAlgorithm ---

    [Test]
    public async Task GetPasswordHashingAlgorithm_BCryptPrefix_ReturnsBCrypt()
    {
        var result = HashingUtils.GetPasswordHashingAlgorithm("bcrypt:$2a$11$...");
        await Assert.That(result).IsEqualTo(PasswordHashingAlgorithm.BCrypt);
    }

    [Test]
    public async Task GetPasswordHashingAlgorithm_Pbkdf2Prefix_ReturnsPBKDF2()
    {
        var result = HashingUtils.GetPasswordHashingAlgorithm("pbkdf2:somehash");
        await Assert.That(result).IsEqualTo(PasswordHashingAlgorithm.PBKDF2);
    }

    [Test]
    public async Task GetPasswordHashingAlgorithm_UnknownPrefix_ReturnsUnknown()
    {
        var result = HashingUtils.GetPasswordHashingAlgorithm("argon2:hash");
        await Assert.That(result).IsEqualTo(PasswordHashingAlgorithm.Unknown);
    }

    [Test]
    public async Task GetPasswordHashingAlgorithm_NoColon_ReturnsUnknown()
    {
        var result = HashingUtils.GetPasswordHashingAlgorithm("nocolonhere");
        await Assert.That(result).IsEqualTo(PasswordHashingAlgorithm.Unknown);
    }

    [Test]
    public async Task GetPasswordHashingAlgorithm_Empty_ReturnsUnknown()
    {
        var result = HashingUtils.GetPasswordHashingAlgorithm("");
        await Assert.That(result).IsEqualTo(PasswordHashingAlgorithm.Unknown);
    }

    // --- HashToken / VerifyToken ---

    [Test]
    public async Task HashToken_ReturnsSha256OfToken()
    {
        var token = "my-api-token";
        var hash = HashingUtils.HashToken(token);
        var expected = HashingUtils.HashSha256(token);
        await Assert.That(hash).IsEqualTo(expected);
    }

    [Test]
    public async Task VerifyToken_CorrectToken_Verified()
    {
        var token = "test-token-123";
        var hash = HashingUtils.HashToken(token);
        var result = HashingUtils.VerifyToken(token, hash);
        await Assert.That(result.Verified).IsTrue();
        await Assert.That(result.NeedsRehash).IsFalse();
    }

    [Test]
    public async Task VerifyToken_WrongToken_NotVerified()
    {
        var hash = HashingUtils.HashToken("correct-token");
        var result = HashingUtils.VerifyToken("wrong-token", hash);
        await Assert.That(result.Verified).IsFalse();
    }

    [Test]
    public async Task VerifyToken_EmptyToken_NotVerified()
    {
        var hash = HashingUtils.HashToken("something");
        var result = HashingUtils.VerifyToken("", hash);
        await Assert.That(result.Verified).IsFalse();
    }

    [Test]
    public async Task VerifyToken_LegacyBcryptHash_NeedsRehash()
    {
        // Legacy tokens stored with bcrypt (contains '$' in hash)
        var token = "legacy-token";
        var bcryptHash = HashingUtils.HashPassword(token);
        var result = HashingUtils.VerifyToken(token, bcryptHash);
        // Whether verified depends on bcrypt, but NeedsRehash should be true
        await Assert.That(result.NeedsRehash).IsTrue();
    }
}