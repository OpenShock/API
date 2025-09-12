using System.Buffers;
using System.Runtime.CompilerServices;

namespace OpenShock.Common.Utils;

public static class DomainUtils
{
    private static readonly SearchValues<char> ValidLabelChars =
        SearchValues.Create("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidDomain(ReadOnlySpan<char> str)
    {
        if (str.Length is 0 or > 253) return false;
        if (str.IndexOf('.') == -1) return str is "localhost";

        foreach (var range in str.Split('.'))
        {
            if (!IsValidLabel(str[range])) return false;
        }

        return true;
    }
    
    public static bool HostMatchesCookieDomainCore(ReadOnlySpan<char> host, ReadOnlySpan<char> cookieDomain)
    {
        var hostLabels = new ReverseLabelEnumerator(host);
        var cookieLabels = new ReverseLabelEnumerator(cookieDomain);

        while (cookieLabels.MoveNext())
        {
            if (!hostLabels.MoveNext()) return false;
            if (!LabelsEqualIgnoreCase(hostLabels.Current, cookieLabels.Current))
                return false;
        }

        // Boundary check
        return !hostLabels.HasRemaining || host[hostLabels.Position + 1] == '.';
    }

    /// <summary>
    /// Returns true if <paramref name="host"/> ends with <paramref name="cookieDomain"/> on a label boundary.
    /// Accepts cookie domains with an optional leading '.' (ignored).
    /// Both must be valid domains (after normalizing the cookie domain).
    /// </summary>
    public static bool HostMatchesCookieDomain(ReadOnlySpan<char> host, ReadOnlySpan<char> cookieDomain)
    {
        cookieDomain = RemoveLeadingDot(cookieDomain);
        
        if (!IsValidDomain(host) || !IsValidDomain(cookieDomain)) return false;
        
        return HostMatchesCookieDomainCore(host, cookieDomain);
    }

    /// <summary>
    /// Picks the most specific matching cookie domain (most labels) from a comma-separated list.
    /// Accepts items with an optional leading '.' and optional ASCII whitespace around them.
    /// </summary>
    public static string? GetBestMatchingCookieDomain(string host, IReadOnlyCollection<string> cookieDomainList)
    {
        ReadOnlySpan<char> hostSpan = host.AsSpan();
        if (!IsValidDomain(hostSpan)) return null;

        string? best = null;
        int bestLabels = -1;

        foreach (var range in cookieDomainList)
        {
            var cd = range.AsSpan().Trim(); // trim ASCII whitespace
            if (cd.Length == 0) continue;

            cd = RemoveLeadingDot(cd); // strip a single leading '.'

            if (!IsValidDomain(cd)) continue;
            // if (IsPublicSuffix(cd)) continue;

            if (!HostMatchesCookieDomain(hostSpan, cd)) continue;

            int labels = cd.Count('.') + 1;
            if (labels <= bestLabels) continue;

            bestLabels = labels;
            best = cd.ToString();
        }

        return best;
    }

    // --- helpers ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<char> RemoveLeadingDot(ReadOnlySpan<char> cd)
    {
        // RFC 6265: a leading dot is ignored (".example.com" == "example.com")
        if (cd.Length > 0 && cd[0] == '.')
            cd = cd[1..];
        return cd;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsValidLabel(ReadOnlySpan<char> label)
    {
        if ((uint)label.Length is 0 or > 63) return false;
        if (label[0] == '-' || label[^1] == '-') return false;
        return !label.ContainsAnyExcept(ValidLabelChars);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool LabelsEqualIgnoreCase(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (ToLowerAscii(a[i]) != ToLowerAscii(b[i])) return false;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static char ToLowerAscii(char c)
        => c is >= 'A' and <= 'Z' ? (char)(c + 32) : c;
}

// Your ReverseLabelEnumerator stays as-is
file ref struct ReverseLabelEnumerator
{
    private readonly ReadOnlySpan<char> _span;

    public ReverseLabelEnumerator(ReadOnlySpan<char> span)
    {
        _span = span;
        Position = span.Length - 1;
        Current = default;
    }

    public ReadOnlySpan<char> Current { get; private set; }
    public int Position { get; private set; }

    public bool HasRemaining => Position >= 0;

    public bool MoveNext()
    {
        if (Position < 0) return false;

        int end = Position;
        while (Position >= 0 && _span[Position] != '.') Position--;

        if (end == Position) return false; // empty label
        Current = _span.Slice(Position + 1, end - Position);
        Position--; // move to char before the dot
        return true;
    }
}
