using System.Diagnostics.CodeAnalysis;

namespace OpenShock.Common.Geo;

public readonly record struct Alpha2CountryCode(char Char1, char Char2)
{
    public static readonly Alpha2CountryCode UnknownCountry = "XX"; // Country code for unknown country

    public static bool TryParseAndValidate(string str, [MaybeNullWhen(false)] out Alpha2CountryCode code)
    {
        if (str.Length != 2 || !char.IsAsciiLetterUpper(str[0]) || !char.IsAsciiLetterUpper(str[1]))
        {
            code = default;
            return false;
        }

        code = new(str[0], str[1]);

        return true;
    }

    public static implicit operator Alpha2CountryCode(string str)
    {
        if (str.Length != 2)
            throw new ArgumentOutOfRangeException(nameof(str), "String input must be exactly 2 chars");

        if (!char.IsAsciiLetterUpper(str[0]) || !char.IsAsciiLetterUpper(str[1]))
            throw new ArgumentOutOfRangeException(nameof(str), "String input must be upper characters only");

        return new Alpha2CountryCode(str[0], str[1]);
    }

    public bool IsUnknown() => this == UnknownCountry;

    public override string ToString() => new([Char1, Char2]);
}