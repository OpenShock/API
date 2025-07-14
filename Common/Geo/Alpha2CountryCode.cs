namespace OpenShock.Common.Geo;

public readonly struct Alpha2CountryCode : IEquatable<Alpha2CountryCode>, IComparable<Alpha2CountryCode>
{
    public static readonly Alpha2CountryCode UnknownCountry = new('X', 'X');

    private readonly ushort _code;
    public char Char1 => (char)(_code >> 8);
    public char Char2 => (char)(_code & 0xFF);

    private Alpha2CountryCode(char c1, char c2)
    {
        _code = (ushort)((c1 << 8) | c2);
    }

    public static Alpha2CountryCode FromString(ReadOnlySpan<char> str)
    {
        if (str is not [>= 'A' and <= 'Z', >= 'A' and <= 'Z'])
            throw new ArgumentOutOfRangeException(nameof(str), "Country code must be exactly 2 uppercase ASCII characters");

        return new Alpha2CountryCode(str[0], str[1]);
    }
    public static Alpha2CountryCode FromString(string str)
    {
        if (str is not [>= 'A' and <= 'Z', >= 'A' and <= 'Z'])
            throw new ArgumentOutOfRangeException(nameof(str), "Country code must be exactly 2 uppercase ASCII characters");

        return new Alpha2CountryCode(str[0], str[1]);
    }

    public static bool TryParse(ReadOnlySpan<char> str, out Alpha2CountryCode code)
    {
        if (str is not [>= 'A' and <= 'Z', >= 'A' and <= 'Z'])
        {
            code = UnknownCountry;
            return false;
        }

        code = FromString(str);
        return true;
    }

    public static int GetCombinedHashCode(Alpha2CountryCode code1, Alpha2CountryCode code2)
    {
        ushort a = code1._code;
        ushort b = code2._code;

        if (a > b) (b, a) = (a,b);

        return (a << 16) | b;
    }

    public bool IsUnknown() => _code == UnknownCountry._code;

    public override int GetHashCode() => _code;

    public override string ToString() => string.Create(2, _code, (span, code) =>
    {
        span[0] = (char)(code >> 8);
        span[1] = (char)(code & 0xFF);
    });

    public bool Equals(Alpha2CountryCode other) => _code == other._code;

    public override bool Equals(object? obj) => obj is Alpha2CountryCode other && Equals(other);

    public int CompareTo(Alpha2CountryCode other) => _code.CompareTo(other._code);

    public static bool operator ==(Alpha2CountryCode left, Alpha2CountryCode right) => left.Equals(right);

    public static bool operator !=(Alpha2CountryCode left, Alpha2CountryCode right) => !left.Equals(right);

    public static bool operator <(Alpha2CountryCode left, Alpha2CountryCode right) => left._code < right._code;

    public static bool operator >(Alpha2CountryCode left, Alpha2CountryCode right) => left._code > right._code;

    public static implicit operator Alpha2CountryCode(ReadOnlySpan<char> str) => FromString(str);
    public static implicit operator Alpha2CountryCode(string str) => FromString(str);
}