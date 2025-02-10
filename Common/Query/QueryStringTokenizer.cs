using System.Text;

namespace OpenShock.Common.Query;

public sealed class QueryStringTokenizerException : Exception
{
    public QueryStringTokenizerException(string message) : base(message) { }
}

public static class QueryStringTokenizer
{
    private const char QuerySpaceChar = ' ';
    private const char QueryQuoteChar = '\'';
    private const char QueryEscapeChar = '\\';

    /// <summary>
    /// Parses a query string into a list of words, handling spaces, quoted strings, and escape sequences.
    /// </summary>
    /// <param name="query">The input query as a <see cref="ReadOnlySpan{char}"/>.</param>
    /// <returns>A list of parsed words from the query.</returns>
    /// <exception cref="ExpressionException">
    /// Thrown when the query contains an invalid escape sequence, an unclosed quoted string, or other syntax errors.
    /// </exception>
    /// <example>
    /// <code>
    /// var result = ParseQueryWords("hello world");
    /// result will contain: ["hello", "world"]
    /// 
    /// var result = ParseQueryWords("'hello world'");
    /// result will contain: ["hello world"]
    /// 
    /// var result = ParseQueryWords("this 'isn\'t invalid'");
    /// result will contain: ["this", "isn't invalid"]
    /// </code>
    /// </example>
    public static List<string> ParseQueryTokens(ReadOnlySpan<char> query)
    {
        query = query.Trim();

        List<string> tokens = [];

        while (!query.IsEmpty)
        {
            int i;
            if (query[0] != QueryQuoteChar)
            {
                i = query.IndexOfAny(QuerySpaceChar, QueryQuoteChar, QueryEscapeChar);
                if (i < 0)
                {
                    // End of query
                    tokens.Add(query.ToString());
                    break;
                }

                // Error on non-space syntax character
                if (query[i] != QuerySpaceChar)
                    throw new QueryStringTokenizerException("Invalid unquoted string in query.");

                // Next space seperated part
                tokens.Add(query[..i].ToString());

                query = query[(i + 1)..].TrimStart();
                continue;
            }

            i = query[1..].IndexOfAny(QueryQuoteChar, QueryEscapeChar) + 1;
            if (i <= 0)
                throw new QueryStringTokenizerException("Closing quote not found.");

            // Fast path: string contains no escapes
            if (query[i] == QueryQuoteChar)
            {
                // If i is 1 then its empty quotes
                tokens.Add(i == 1 ? string.Empty : query[1..i].ToString());
                query = query[(i + 1)..].TrimStart();
                continue;
            }

            // Otherwise, fall back to the slower character-by-character parse.
            var sb = new StringBuilder();

            while (true)
            {
                // Add everything before escape
                if (i > 0) sb.Append(query[..i]);

                // Needs space for escape sequence and end of string
                if (i + 3 >= query.Length)
                    throw new QueryStringTokenizerException("Invalid end of query.");

                // Add escape
                sb.Append(query[++i] switch
                {
                    QueryQuoteChar => QueryQuoteChar,
                    QueryEscapeChar => QueryEscapeChar,
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    _ => throw new QueryStringTokenizerException("Invalid escape sequence.")
                });

                // Skip past escape sequence
                query = query[(i + 1)..].TrimStart();

                i = query.IndexOfAny(QueryQuoteChar, QueryEscapeChar);
                if (i <= 0) throw new QueryStringTokenizerException("Closing quote not found.");

                if (query[i] == QueryQuoteChar)
                {
                    // Add everything before quote
                    if (i > 0) sb.Append(query[..i]);

                    // Finish off string
                    tokens.Add(sb.ToString());
                    break;
                }

                // Loop continues at escape found
            }
        }

        return tokens;
    }
}
