using OpenShock.Common.Query;

namespace OpenShock.Common.Tests.Query;
public class QueryStringTokenizerTests
{
    [Test]
    public async Task SimpleString_ReturnsMatching()
    {
        // Act
        var result = QueryStringTokenizer.ParseQueryTokens("testing");

        // Assert
        await Assert.That(result).IsEqualTo(["testing"]);
    }

    [Test]
    public async Task SpaceSeperatedString_ReturnsMatching()
    {
        // Act
        var result = QueryStringTokenizer.ParseQueryTokens("testing tokenizer");

        // Assert
        await Assert.That(result).IsEqualTo(["testing", "tokenizer"]);
    }

    [Test]
    public async Task QuotedString_ReturnsMatching()
    {
        // Act
        var result = QueryStringTokenizer.ParseQueryTokens("'testing tokenizer'");

        // Assert
        await Assert.That(result).IsEqualTo(["testing tokenizer"]);
    }

    [Test]
    public async Task QuotedAndEscapedString_ReturnsMatching()
    {
        // Act
        var result = QueryStringTokenizer.ParseQueryTokens("'this shouldn\'t fail'");

        // Assert
        await Assert.That(result).IsEqualTo(["this shouldn't fail"]);
    }
}