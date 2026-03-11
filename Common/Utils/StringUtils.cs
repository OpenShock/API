using System.Diagnostics.CodeAnalysis;

namespace OpenShock.Common.Utils;

public static class StringUtils
{
    public static string Truncate(this string input, int maxLength)
    {
        return input.Length <= maxLength ? input : input[..maxLength];
    }
    public static bool TryRemoveSuffix(string str, string suffix, [NotNullWhen(true)] out string? value)
    {
        if (!str.EndsWith(suffix))
        {
            value = null;
            return false;
        }
        
        value = str[..^suffix.Length];
        return true;
    }
}