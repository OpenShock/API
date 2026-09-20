using System.Buffers;

namespace OpenShock.Common.Utils;

public static class StringUtils
{
    public static string Truncate(this string input, int maxLength)
    {
        return input.Length <= maxLength ? input : input[..maxLength];
    }
    
    public static string RemoveConsecutiveSpaces(ReadOnlySpan<char> input)
    {
        var array = ArrayPool<char>.Shared.Rent(input.Length);

        int i = 0;
        bool inWhitespace = false;

        foreach (var c in input)
        {
            if (char.IsWhiteSpace(c)) // Or use c == ' ' if only tracking standard spaces
            {
                if (inWhitespace) continue;
                
                array[i++] = ' '; // Collapse to a single space
                inWhitespace = true;
            }
            else
            {
                array[i++] = c;
                inWhitespace = false;
            }
        }

        string result = new string(array.AsSpan(0, i));
        ArrayPool<char>.Shared.Return(array);
        return result;
    }
}