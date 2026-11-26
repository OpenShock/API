namespace OpenShock.Common.Utils;

public static class StringUtils
{
    public static string Truncate(this string input, int maxLength)
    {
        if (input.Length <= maxLength) return input;

        // Never cut a surrogate pair in half, that would leave an invalid UTF-16 string.
        if (maxLength > 0 && char.IsHighSurrogate(input[maxLength - 1])) maxLength--;

        return input[..maxLength];
    }

    /// <summary>
    /// Reads a loosely typed boolean, as environment variables and OAuth provider claims carry them:
    /// <c>1</c>, <c>true</c> and <c>yes</c> mean true, in any casing and with surrounding whitespace.
    /// Everything else, including null and empty, is false.
    /// </summary>
    public static bool IsTruthy(string? value)
    {
        if (value is null) return false;

        var trimmed = value.AsSpan().Trim();
        return trimmed.Equals("1", StringComparison.Ordinal)
            || trimmed.Equals("true", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }
}
