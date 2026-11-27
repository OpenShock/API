using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using OpenShock.Common.Constants;
using OpenShock.Internal.Common.Constants;

namespace OpenShock.Common.Validation;

/// <summary>
/// The single definition of what counts as an email address and an email domain. Only plain ASCII addresses are
/// supported: a local part of RFC 5322 atext and dots (no quoted strings, comments or display names) and an LDH domain
/// (RFC 5321, no IP literals, no internationalised names).
/// </summary>
/// <remarks>
/// Works on spans and allocates only when producing a normalised string, so it is cheap enough to run over
/// pasted lists of tens of thousands of entries.
/// </remarks>
public static class EmailValidator
{
    public const int LocalPartMaxLength = 64;
    public const int DomainLabelMaxLength = 63;

    /// <summary>
    /// Whether <paramref name="address"/> is a supported email address, as written: no surrounding whitespace.
    /// </summary>
    public static bool IsValidAddress(ReadOnlySpan<char> address) => TrySplitAddress(address, out _, out _);

    /// <summary>
    /// Validates <paramref name="address"/> and returns its local part and domain.
    /// </summary>
    public static bool TrySplitAddress(ReadOnlySpan<char> address, out ReadOnlySpan<char> localPart, out ReadOnlySpan<char> domain)
    {
        localPart = default;
        domain = default;

        if (address.Length is < HardLimits.EmailAddressMinLength or > HardLimits.EmailAddressMaxLength) return false;

        var at = address.IndexOf('@');
        if (at < 0) return false;

        var local = address[..at];
        var host = address[(at + 1)..];

        if (!IsValidLocalPart(local) || !IsValidDomain(host)) return false;

        localPart = local;
        domain = host;
        return true;
    }

    /// <summary>
    /// Validates <paramref name="address"/> and returns it in the canonical form addresses are stored and compared
    /// in: lowercased.
    /// </summary>
    public static bool TryNormalizeAddress(ReadOnlySpan<char> address, [NotNullWhen(true)] out string? normalized)
    {
        normalized = null;
        if (!IsValidAddress(address)) return false;

        normalized = ToLowerAscii(address);
        return true;
    }

    /// <summary>
    /// Whether <paramref name="domain"/> is a supported mail domain: at least two labels (no bare "localhost"),
    /// each 1 to 63 letters, digits or hyphens that doesn't start or end with a hyphen, a top label that isn't all
    /// digits (so IPv4 literals don't pass), and at most 255 characters in total.
    /// </summary>
    public static bool IsValidDomain(ReadOnlySpan<char> domain)
    {
        if (domain.Length is 0 or > ApiHardLimits.EmailProviderDomainMaxLength) return false;

        var labelCount = 0;

        while (true)
        {
            var dot = domain.IndexOf('.');
            var label = dot < 0 ? domain : domain[..dot];

            if (label.Length is 0 or > DomainLabelMaxLength || label[0] == '-' || label[^1] == '-') return false;
            if (label.ContainsAnyExcept(DomainLabelChars)) return false;
            labelCount++;

            if (dot < 0) return labelCount >= 2 && label.ContainsAnyExceptInRange('0', '9');

            domain = domain[(dot + 1)..];
        }
    }

    /// <summary>
    /// Turns user-entered input naming a mail domain into the domain addresses are compared against. Accepts a bare
    /// domain or a full address (whose domain is used), tolerating surrounding whitespace and a trailing root dot,
    /// and returns it lowercased.
    /// </summary>
    public static bool TryNormalizeDomain(ReadOnlySpan<char> input, [NotNullWhen(true)] out string? domain)
    {
        domain = null;

        var span = input.Trim();

        var at = span.LastIndexOf('@');
        if (at >= 0) span = span[(at + 1)..];

        if (span is [.., '.']) span = span[..^1];

        if (!IsValidDomain(span)) return false;

        domain = ToLowerAscii(span);
        return true;
    }

    /// <summary>
    /// RFC 5322 atext and dots, at most 64 characters. Dots may appear anywhere, as in the WHATWG definition browsers
    /// use for input type=email: strict dot-atom forbids leading, trailing and doubled dots, but Japanese carriers
    /// (docomo.ne.jp, ezweb.ne.jp, ...) issued such addresses and they are still in use.
    /// </summary>
    private static bool IsValidLocalPart(ReadOnlySpan<char> localPart)
    {
        if (localPart.Length is 0 or > LocalPartMaxLength) return false;

        return !localPart.ContainsAnyExcept(LocalPartChars);
    }

    private static string ToLowerAscii(ReadOnlySpan<char> value)
    {
        Span<char> lower = value.Length <= 512 ? stackalloc char[value.Length] : new char[value.Length];
        Ascii.ToLower(value, lower, out _);
        return new string(lower);
    }

    private static readonly SearchValues<char> DomainLabelChars =
        SearchValues.Create("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-");

    // RFC 5322 atext plus the dot separating atoms
    private static readonly SearchValues<char> LocalPartChars =
        SearchValues.Create("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!#$%&'*+-/=?^_`{|}~.");
}
