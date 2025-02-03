using OpenShock.Common.Utils;

namespace OpenShock.Common.Tests.Utils;

public class HashingUtilsTests
{
    [Test]
    [Arguments("test", "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08")]
    [Arguments("Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in", "2fac5f5f1d048a84fbb75c389f4596e05023ac17da4fcf45a5954d2d9a394301")]
    public async Task HashSha256(string str, string expectedHash)
    {
        // Act
        var result = HashingUtils.HashSha256(str);

        // Assert
        await Assert.That(result).IsEqualTo(expectedHash);
    }
}