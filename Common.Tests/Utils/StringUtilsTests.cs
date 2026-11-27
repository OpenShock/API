using OpenShock.Common.Utils;

namespace OpenShock.Common.Tests.Utils;

public sealed class StringUtilsTests
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

    [Test]
    public async Task Truncate_CutInsideSurrogatePair_DropsWholePair()
    {
        var result = "abc😀".Truncate(4);
        await Assert.That(result).IsEqualTo("abc");
    }

    [Test]
    public async Task Truncate_CutAfterSurrogatePair_KeepsPair()
    {
        var result = "abc😀def".Truncate(5);
        await Assert.That(result).IsEqualTo("abc😀");
    }

    [Test]
    [Arguments("1")]
    [Arguments("true")]
    [Arguments("True")]
    [Arguments("TRUE")]
    [Arguments("yes")]
    [Arguments("Yes")]
    [Arguments("  true \t")]
    public async Task IsTruthy_TruthyValues_ReturnsTrue(string value)
    {
        await Assert.That(StringUtils.IsTruthy(value)).IsTrue();
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments("0")]
    [Arguments("false")]
    [Arguments("no")]
    [Arguments("on")]
    [Arguments("2")]
    [Arguments("11")]
    [Arguments("truthy")]
    public async Task IsTruthy_AnythingElse_ReturnsFalse(string? value)
    {
        await Assert.That(StringUtils.IsTruthy(value)).IsFalse();
    }
}
