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

    [Test]
    public async Task RemoveConsecutiveSpaces_EmptyInput_ReturnsEmpty()
    {
        var result = StringUtils.RemoveConsecutiveSpaces("");
        await Assert.That(result).IsEqualTo("");
    }

    [Test]
    public async Task RemoveConsecutiveSpaces_NoWhitespaceRuns_ReturnsOriginal()
    {
        var result = StringUtils.RemoveConsecutiveSpaces("hello world");
        await Assert.That(result).IsEqualTo("hello world");
    }

    [Test]
    public async Task RemoveConsecutiveSpaces_MultipleSpaces_CollapsesToOne()
    {
        var result = StringUtils.RemoveConsecutiveSpaces("a   b    c");
        await Assert.That(result).IsEqualTo("a b c");
    }

    [Test]
    public async Task RemoveConsecutiveSpaces_MixedSpacesTabsNewlines_CollapsesToSingleSpace()
    {
        var result = StringUtils.RemoveConsecutiveSpaces("a \t\r\n  b\n\nc\td");
        await Assert.That(result).IsEqualTo("a b c d");
    }

    [Test]
    public async Task RemoveConsecutiveSpaces_LeadingAndTrailingWhitespace_CollapsedNotTrimmed()
    {
        var result = StringUtils.RemoveConsecutiveSpaces("  \n hello \t\n ");
        await Assert.That(result).IsEqualTo(" hello ");
    }

    [Test]
    public async Task RemoveConsecutiveSpaces_OnlyWhitespace_ReturnsSingleSpace()
    {
        var result = StringUtils.RemoveConsecutiveSpaces(" \t\r\n ");
        await Assert.That(result).IsEqualTo(" ");
    }

    [Test]
    public async Task RemoveConsecutiveSpaces_UnicodeWhitespace_CollapsesToAsciiSpace()
    {
        // NBSP, EM SPACE, IDEOGRAPHIC SPACE
        var result = StringUtils.RemoveConsecutiveSpaces("a  　b");
        await Assert.That(result).IsEqualTo("a b");
    }
}
