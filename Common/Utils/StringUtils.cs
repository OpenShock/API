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
}
