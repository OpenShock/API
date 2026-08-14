using OpenShock.Common.Utils;

namespace OpenShock.Common.Tests.Utils;

public class StringUtilsTests
{
    [Test]
    public async Task Truncate_ShorterThanMax_ReturnsOriginal()
    {
        var result = "hello".Truncate(10);
        await Assert.That(result).IsEqualTo("hello");
    }

    [Test]
    public async Task Truncate_ExactLength_ReturnsOriginal()
    {
        var result = "hello".Truncate(5);
        await Assert.That(result).IsEqualTo("hello");
    }

    [Test]
    public async Task Truncate_LongerThanMax_Truncates()
    {
        var result = "hello world".Truncate(5);
        await Assert.That(result).IsEqualTo("hello");
    }

    [Test]
    public async Task Truncate_EmptyString_ReturnsEmpty()
    {
        var result = "".Truncate(5);
        await Assert.That(result).IsEqualTo("");
    }

    [Test]
    public async Task Truncate_MaxZero_ReturnsEmpty()
    {
        var result = "hello".Truncate(0);
        await Assert.That(result).IsEqualTo("");
    }

    [Test]
    public async Task Truncate_MaxOne_ReturnsSingleChar()
    {
        var result = "hello".Truncate(1);
        await Assert.That(result).IsEqualTo("h");
    }
}
