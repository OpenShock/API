using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using MessagePack;

namespace OpenShock.Common.Models;

/// <summary>
/// Lightweight semantic version model with optional prerelease and build metadata.
/// </summary>
/// <remarks>
/// - Accepts versions in the form <c>MAJOR[.MINOR[.PATCH]][-PRERELEASE][+BUILD]</c>.
/// - Numeric parts are parsed into <see cref="ushort"/>.
/// - Leading/trailing whitespace is trimmed.
/// - Total input length is capped by <see cref="MaxLength"/>.
/// - This is intentionally permissive and not a full SemVer 2.0 validator.
/// </remarks>
[MessagePackObject]
public sealed class SemVersion : IEquatable<SemVersion>
{
    /// <summary>Maximum allowed length of a version string.</summary>
    public const int MaxLength = 1024;

    #region Construction

    /// <summary>
    /// Initializes a new instance of <see cref="SemVersion"/>.
    /// </summary>
    public SemVersion(ushort major, ushort minor, ushort patch, string? prerelease = null, string? build = null)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        Prerelease = prerelease;
        Build = build;
    }

    /// <summary>
    /// Creates a <see cref="SemVersion"/> from a FlatBuffers <c>Serialization.Types.SemVer</c> instance.
    /// </summary>
    public static SemVersion FromFbs(Serialization.Types.SemVer fbs) =>
        new(fbs.Major, fbs.Minor, fbs.Patch, fbs.Prerelease, fbs.Build);

    /// <summary>
    /// Converts this instance to a FlatBuffers <c>Serialization.Types.SemVer</c>.
    /// </summary>
    public Serialization.Types.SemVer ToFbs() => new()
    {
        Major = Major,
        Minor = Minor,
        Patch = Patch,
        Prerelease = Prerelease,
        Build = Build
    };

    #endregion

    #region Parsing

    /// <summary>
    /// Parses a version string or throws <see cref="FormatException"/> on failure.
    /// </summary>
    public static SemVersion Parse(string version) =>
        TryParse(version, out var result)
            ? result
            : throw new FormatException("Invalid version format.");

    /// <summary>
    /// Parses a version span or throws <see cref="FormatException"/> on failure.
    /// </summary>
    public static SemVersion Parse(ReadOnlySpan<char> span) =>
        TryParse(span, out var result)
            ? result
            : throw new FormatException("Invalid version format.");

    /// <summary>
    /// Attempts to parse a version string.
    /// </summary>
    public static bool TryParse(string? version, [NotNullWhen(true)] out SemVersion? value)
    {
        if (string.IsNullOrEmpty(version))
        {
            value = null;
            return false;
        }

        return TryParse(version.AsSpan(), out value);
    }
    
    /// <summary>
    /// Strictly parses a numeric identifier (MAJOR, MINOR, PATCH) into a <see cref="ushort"/>.
    /// - Only digits 0–9 allowed
    /// - "0" allowed, otherwise no leading zeros
    /// - Must fit into <see cref="ushort"/>
    /// </summary>
    private static bool TryParseUShortStrict(ReadOnlySpan<char> span, out ushort value)
    {
        value = 0;

        // Empty not allowed
        if (span.IsEmpty)
            return false;

        // Only digits
        foreach (var c in span)
            if (c is < '0' or > '9')
                return false;

        // Leading zero rule
        if (span.Length > 1 && span[0] == '0')
            return false;

        // Actual parse
        return ushort.TryParse(span, out value);
    }

    /// <summary>
    /// Attempts to parse a version span.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> span, [NotNullWhen(true)] out SemVersion? semVersion)
    {
        semVersion = null;

        if (span.IsEmpty || span.Length > MaxLength)
            return false;

        string? prerelease = null;
        string? build = null;

        // Extract build metadata: everything after the first '+'
        int plusIndex = span.IndexOf('+');
        if (plusIndex >= 0)
        {
            if (plusIndex == span.Length - 1) return false; // '+' with nothing after
            build = span[(plusIndex + 1)..].ToString();
            span = span[..plusIndex];
        }

        // Extract prerelease: everything after the first '-'
        int dashIndex = span.IndexOf('-');
        if (dashIndex >= 0)
        {
            if (dashIndex == span.Length - 1) return false; // '-' with nothing after
            prerelease = span[(dashIndex + 1)..].ToString();
            span = span[..dashIndex];
        }

        // Split into major[.minor[.patch]]
        if (!TrySplitVersionParts(span, out var majorSpan, out var minorSpan, out var patchSpan, out int count))
            return false;

        ushort major, minor = 0, patch = 0;

        // Parse optional parts depending on count
        switch (count)
        {
            case 1:
                if (!TryParseUShortStrict(majorSpan, out major)) return false;
                break;
            case 2:
                if (!TryParseUShortStrict(majorSpan, out major)) return false;
                if (!TryParseUShortStrict(minorSpan, out minor)) return false;
                break;
            case 3:
                if (!TryParseUShortStrict(majorSpan, out major)) return false;
                if (!TryParseUShortStrict(minorSpan, out minor)) return false;
                if (!TryParseUShortStrict(patchSpan, out patch)) return false;
                break;
            default:
                return false;
        }

        semVersion = new SemVersion(major, minor, patch, prerelease, build);
        return true;
    }

    /// <summary>
    /// Splits <c>A[.B[.C]]</c> into up to 3 parts without allocations.
    /// Returns false if there are &gt;2 dots or any empty segment.
    /// </summary>
    private static bool TrySplitVersionParts(
        ReadOnlySpan<char> s,
        out ReadOnlySpan<char> a,
        out ReadOnlySpan<char> b,
        out ReadOnlySpan<char> c,
        out int count)
    {
        a = b = c = default;
        count = 0;

        if (s.IsEmpty) return false;

        int firstDot = s.IndexOf('.');
        if (firstDot < 0)
        {
            a = s;
            count = 1;
            return !a.IsEmpty;
        }

        // A . B[.C]
        if (firstDot == 0 || firstDot == s.Length - 1) return false;
        a = s[..firstDot];

        var rest = s[(firstDot + 1)..];
        int secondDot = rest.IndexOf('.');
        if (secondDot < 0)
        {
            if (rest.IsEmpty) return false;
            b = rest;
            count = 2;
            return true;
        }

        // A . B . C
        if (secondDot == 0 || secondDot == rest.Length - 1) return false;
        b = rest[..secondDot];
        c = rest[(secondDot + 1)..];

        // Ensure no more dots
        if (c.IndexOf('.') >= 0) return false;

        count = 3;
        return true;
    }

    #endregion

    #region Data

    [Key(0)] public ushort Major { get; init; }
    [Key(1)] public ushort Minor { get; init; }
    [Key(2)] public ushort Patch { get; init; }
    [Key(3)] public string? Prerelease { get; init; }
    [Key(4)] public string? Build { get; init; }

    #endregion

    #region Formatting

    /// <inheritdoc />
    public override string ToString()
    {
        var sb = new StringBuilder(16);

        sb.Append(Major);
        sb.Append('.');
        sb.Append(Minor);
        sb.Append('.');
        sb.Append(Patch);

        if (!string.IsNullOrEmpty(Prerelease))
        {
            sb.Append('-');
            sb.Append(Prerelease);
        }

        if (!string.IsNullOrEmpty(Build))
        {
            sb.Append('+');
            sb.Append(Build);
        }

        return sb.ToString();
    }

    #endregion

    #region Equality

    /// <inheritdoc />
    public bool Equals(SemVersion? other) =>
        other is not null &&
        Major == other.Major &&
        Minor == other.Minor &&
        Patch == other.Patch &&
        string.Equals(Prerelease, other.Prerelease, StringComparison.Ordinal) &&
        string.Equals(Build, other.Build, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is SemVersion other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, Prerelease, Build);

    #endregion
}
