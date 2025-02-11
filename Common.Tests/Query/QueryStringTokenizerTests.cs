using OpenShock.Common.Query;

namespace OpenShock.Common.Tests.Query;
public class QueryStringTokenizerTests
{
    [Test]
    public async Task EmptyString_ReturnsEmpty()
    {
        // Act
        var result = QueryStringTokenizer.ParseQueryTokens("");

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task WhiteSpaceString_ReturnsEmpty()
    {
        // Act
        var result = QueryStringTokenizer.ParseQueryTokens(" \r\n\t");

        // Assert
        await Assert.That(result).IsEmpty();
    }
    
    [Test]
    public async Task QuotedNewLine_ReturnsNewLine()
    { 
        // Act
        var result = QueryStringTokenizer.ParseQueryTokens("'\n'");

        // Assert
        await Assert.That(result).IsEquivalentTo(["\n"]);
    }

    [Test]
    public async Task SimpleString_ReturnsMatching()
    {
        // Act
        var result = QueryStringTokenizer.ParseQueryTokens("testing");

        // Assert
        await Assert.That(result).IsEquivalentTo(["testing"]);
    }

    [Test]
    public async Task NormalUsage_Succeeds()
    {
        // Act
        var result = QueryStringTokenizer.ParseQueryTokens("username == 'morgan freeman' and age >= 35 and email ilike morgan*freeman@*.com");

        // Assert
        await Assert.That(result).IsEquivalentTo(["username", "==", "morgan freeman", "and", "age", ">=", "35", "and", "email", "ilike", "morgan*freeman@*.com"]);
    }

    [Test]
    public async Task SurroundingWhitespace_Ignored()
    {
        // Act
        var result = QueryStringTokenizer.ParseQueryTokens("  hello  ");

        // Assert
        await Assert.That(result).IsEquivalentTo(["hello"]);
    }

    [Test]
    public async Task SpaceSeperatedString_ReturnsMatching()
    {
        // Act
        var result = QueryStringTokenizer.ParseQueryTokens("testing tokenizer");

        // Assert
        await Assert.That(result).IsEquivalentTo(["testing", "tokenizer"]);
    }

    [Test]
    public async Task MultiSpaceSeperatedString_ReturnsMatching()
    {
        // Act
        var result = QueryStringTokenizer.ParseQueryTokens("testing \r \t \n  tokenizer");

        // Assert
        await Assert.That(result).IsEquivalentTo(["testing", "tokenizer"]);
    }

    [Test]
    public async Task UnmatchedQuote_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<QueryStringTokenizerException>(async () =>
        {
            QueryStringTokenizer.ParseQueryTokens("'hello world");
        });
    }

    [Test]
    public async Task EmptyQuotedString_ParsesAsEmpty()
    {
        // Act
        var result = QueryStringTokenizer.ParseQueryTokens("''");

        // Assert
        await Assert.That(result).IsEquivalentTo([string.Empty]);
    }

    [Test]
    public async Task QuotedString_ReturnsMatching()
    {
        // Act
        var result = QueryStringTokenizer.ParseQueryTokens("'testing tokenizer'");

        // Assert
        await Assert.That(result).IsEquivalentTo(["testing tokenizer"]);
    }

    [Test]
    public async Task MixedQuotedAndUnquotedWords_ParsesCorrectly()
    {
        // Act
        var result = QueryStringTokenizer.ParseQueryTokens("this 'is a test' string");

        // Assert
        await Assert.That(result).IsEquivalentTo(["this", "is a test", "string"]);
    }

    [Test]
    public async Task EscapedQuoteInsideQuotedString_ParsesCorrectly()
    {
        // Act
        var result = QueryStringTokenizer.ParseQueryTokens("'This isn\\'t a bug'");

        // Assert
        await Assert.That(result).IsEquivalentTo(["This isn't a bug"]);
    }

    [Test]
    public async Task EscapeAtEndOfQuotedString_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<QueryStringTokenizerException>(async () =>
        {
            QueryStringTokenizer.ParseQueryTokens("'hello world\\'");
        });
    }

    [Test]
    public async Task DoubleEscapedBackslash_ParsesCorrectly()
    {
        // Act
        var result = QueryStringTokenizer.ParseQueryTokens("'This has a backslash: \\\\'");

        // Assert
        await Assert.That(result).IsEquivalentTo(["This has a backslash: \\"]);
    }

    [Test]
    public async Task QuoteInsideUnquotedString_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<QueryStringTokenizerException>(async () =>
        {
            QueryStringTokenizer.ParseQueryTokens("This won't work");
        });
    }

    [Test]
    public async Task UnquotedEscapeCharacter_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<QueryStringTokenizerException>(async () =>
        {
            QueryStringTokenizer.ParseQueryTokens("hello \\ world");
        });
    }

    [Test]
    public async Task OnlyEscapeCharacter_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<QueryStringTokenizerException>(async () =>
        {
            QueryStringTokenizer.ParseQueryTokens("\\");
        });
    }

    [Test]
    public async Task EmbeddedEscapedNewline_ParsesCorrectly()
    {
        // Act
        var result = QueryStringTokenizer.ParseQueryTokens("'hello\\nworld'");

        // Assert
        await Assert.That(result).IsEquivalentTo(["hello\nworld"]);
    }

    [Test]
    public async Task ConsecutiveQuotedStrings_ParsesSeparately()
    {
        // Act
        var result = QueryStringTokenizer.ParseQueryTokens("'hello' 'world'");

        // Assert
        await Assert.That(result).IsEquivalentTo(["hello", "world"]);
    }

    [Test]
    public async Task EmptyInputWithWhitespace_ReturnsEmpty()
    {
        // Act
        var result = QueryStringTokenizer.ParseQueryTokens("     ");

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    [Arguments("'\\ '")] // Escape followed by space
    [Arguments("'hello \\q'")] // Invalid escape character
    [Arguments("'\\x'")] // Undefined escape sequence
    [Arguments("'test \\u1234'")] // Unicode escape not supported
    [Arguments("'hello \\'")] // Dangling backslash at end of quoted string
    public async Task InvalidEscapeCharacters_ThrowsException(string invalidString)
    {
        // Act & Assert
        await Assert.ThrowsAsync<QueryStringTokenizerException>(async () =>
        {
            QueryStringTokenizer.ParseQueryTokens(invalidString);
        });
    }
}