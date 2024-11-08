using System.Diagnostics.CodeAnalysis;

namespace OpenShock.Common.Geo;

public readonly record struct Alpha2CountryCode(char Char1, char Char2)
{
    public static readonly Alpha2CountryCode UnknownCountry = "XX"; // Country code for unknown country

    public static bool TryParseAndValidate(string str, [NotNullWhen(true)] out Alpha2CountryCode code)
    {
        if (str.Length != 2 || !char.IsAsciiLetterUpper(str[0]) || !char.IsAsciiLetterUpper(str[1]))
        {
            code = UnknownCountry;
            return false;
        }

        code = new(str[0], str[1]);

        return true;
    }

    public static implicit operator Alpha2CountryCode(string str)
    {
        if (str.Length != 2)
            throw new ArgumentOutOfRangeException(nameof(str), "Country code must be exactly 2 characters long");

        if (!char.IsAsciiLetterUpper(str[0]) || !char.IsAsciiLetterUpper(str[1]))
            throw new ArgumentOutOfRangeException(nameof(str), "Country code must be uppercase ASCII characters only");

        return new Alpha2CountryCode(str[0], str[1]);
    }

    public static int GetCombinedHashCode(Alpha2CountryCode code1, Alpha2CountryCode code2)
    {
        int a = code1.GetHashCode();
        int b = code2.GetHashCode();

        int v = (a << 16) | b;

        if (a > b) v = int.RotateLeft(v, 16);

        return v;
    }

    public bool IsUnknown() => this == UnknownCountry;

    public override int GetHashCode() => (Char1 << 8) | Char2;
    public override string ToString() => new([Char1, Char2]);
}