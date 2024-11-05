namespace OpenShock.Common.Utils;

public static class StringUtils
{
    public static string Truncate(this string input, int maxLength)
    {
        return input.Length <= maxLength ? input : input[..maxLength];
    }
}