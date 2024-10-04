using OpenShock.Common.Utils;

namespace OpenShock.Common.Tests.Utils;

public class HashingUtilsTests
{
    [Test]
    [Arguments("test", "9F86D081884C7D659A2FEAA0C55AD015A3BF4F1B2B0B822CD15D6C15B0F00A08")]
    public async Task HashSha256(string str, string expectedHash)
    {
        // Act
        var result = HashingUtils.HashSha256(str);

        // Assert
        await Assert.That(result).IsEqualTo(expectedHash);
    }
}