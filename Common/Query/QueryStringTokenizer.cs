using System.Buffers;
using System.Text;

namespace OpenShock.Common.Query;

public sealed class QueryStringTokenizerException : Exception
{
    public QueryStringTokenizerException(string message) : base(message) { }
}

public static class QueryStringTokenizer
{
    private const char QueryQuoteChar = '\'';
    private const char QueryEscapeChar = '\\';

    // In unquoted strings, search for quotes and escapes. If these are found we should fail the parsing.
    private static readonly SearchValues<char> UnquotedSearchValues = SearchValues.Create(' ', '\r', '\n', '\t', QueryQuoteChar, QueryEscapeChar);

    /// <summary>
    /// Parses a query string into a list of words, handling spaces, quoted strings, and escape sequences.
    /// </summary>
    /// <param name="query">The input query as a <see cref="ReadOnlySpan{char}"/>.</param>
    /// <returns>A list of parsed words from the query.</returns>
    /// <exception cref="QueryStringTokenizerException">
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
                i = query.IndexOfAny(UnquotedSearchValues);
                if (i < 0)
                {
                    // End of query
                    tokens.Add(query.ToString());
                    break;
                }

                // Error on non-whitespace syntax character
                if (!char.IsWhiteSpace(query[i]))
                    throw new QueryStringTokenizerException("Invalid unquoted string in query.");

                // Next space seperated part
                tokens.Add(query[..i].ToString());

                query = query[(i + 1)..].TrimStart();
                continue;
            }

            // Skip quote char
            query = query[1..];

            // Find next quote or escape char
            i = query.IndexOfAny(QueryQuoteChar, QueryEscapeChar);
            if (i < 0)
                throw new QueryStringTokenizerException("Closing quote not found.");

            // Fast path: string contains no escapes
            if (query[i] == QueryQuoteChar)
            {
                // If i is 1 then its empty quotes
                tokens.Add(i == 0 ? string.Empty : query[..i].ToString());
                query = query[(i + 1)..].TrimStart();
                continue;
            }

            var sb = new StringBuilder();

            // Parse escaped string
            while (true)
            {
                // Add everything before escape
                if (i > 0) sb.Append(query[..i]);

                // Needs space for escape sequence and end of string
                if (i + 2 >= query.Length)
                    throw new QueryStringTokenizerException("Invalid end of query.");

                // Add escape
                sb.Append(query[i + 1] switch
                {
                    QueryQuoteChar => QueryQuoteChar,
                    QueryEscapeChar => QueryEscapeChar,
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    _ => throw new QueryStringTokenizerException("Invalid escape sequence.")
                });

                // Skip past escape sequence
                query = query[(i + 2)..];

                i = query.IndexOfAny(QueryQuoteChar, QueryEscapeChar);
                if (i < 0)
                    throw new QueryStringTokenizerException("Closing quote not found.");

                if (query[i] == QueryQuoteChar)
                {
                    // Add everything before quote
                    if (i > 0) sb.Append(query[..i]);

                    // Finish off string
                    tokens.Add(sb.ToString());

                    query = query[(i + 1)..].TrimStart();
                    break;
                }

                // Loop continues at escape found
            }
        }

        return tokens;
    }
}
