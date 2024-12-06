using OpenShock.Common.Utils;

namespace OpenShock.Common.Tests.Utils;

public class HashingUtilsTests
{
    [Test]
    [Arguments("test", "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08")]
    public async Task HashSha256(string str, string expectedHash)
    {
        // Act
        var result = HashingUtils.HashSha256(str);

        // Assert
        await Assert.That(result).IsEqualTo(expectedHash);
    }
}