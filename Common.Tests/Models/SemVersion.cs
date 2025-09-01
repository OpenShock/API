using System.Text;
using OpenShock.Common.Models;

namespace OpenShock.Common.Tests.Models;

public class SemVersionTests
{
    // ---------------------------
    // Parse / TryParse - happy path
    // ---------------------------

    [Test]
    public async Task TryParse_CoreOnly_OnePart_Succeeds()
    {
        var ok = SemVersion.TryParse("1", out var v);
        await Assert.That(ok).IsTrue();
        await Assert.That(v!.Major).IsEqualTo((ushort)1);
        await Assert.That(v.Minor).IsEqualTo((ushort)0);
        await Assert.That(v.Patch).IsEqualTo((ushort)0);
        await Assert.That(v.Prerelease).IsNull();
        await Assert.That(v.Build).IsNull();
    }

    [Test]
    public async Task TryParse_CoreOnly_TwoParts_Succeeds()
    {
        var ok = SemVersion.TryParse("1.2", out var v);
        await Assert.That(ok).IsTrue();
        await Assert.That(v!.Major).IsEqualTo((ushort)1);
        await Assert.That(v.Minor).IsEqualTo((ushort)2);
        await Assert.That(v.Patch).IsEqualTo((ushort)0);
    }

    [Test]
    public async Task TryParse_CoreOnly_ThreeParts_Succeeds()
    {
        var ok = SemVersion.TryParse("1.2.3", out var v);
        await Assert.That(ok).IsTrue();
        await Assert.That(v!.Major).IsEqualTo((ushort)1);
        await Assert.That(v.Minor).IsEqualTo((ushort)2);
        await Assert.That(v.Patch).IsEqualTo((ushort)3);
    }

    [Test]
    public async Task TryParse_WithPrerelease_Succeeds()
    {
        var ok = SemVersion.TryParse("1.2.3-alpha.1", out var v);
        await Assert.That(ok).IsTrue();
        await Assert.That(v!.Prerelease).IsEqualTo("alpha.1");
        await Assert.That(v.Build).IsNull();
    }

    [Test]
    public async Task TryParse_WithBuild_Succeeds()
    {
        var ok = SemVersion.TryParse("1.2.3+build.5", out var v);
        await Assert.That(ok).IsTrue();
        await Assert.That(v!.Build).IsEqualTo("build.5");
        await Assert.That(v.Prerelease).IsNull();
    }

    [Test]
    public async Task TryParse_WithPrereleaseAndBuild_Succeeds()
    {
        var ok = SemVersion.TryParse("1.2.3-rc.2+exp.sha", out var v);
        await Assert.That(ok).IsTrue();
        await Assert.That(v!.Prerelease).IsEqualTo("rc.2");
        await Assert.That(v.Build).IsEqualTo("exp.sha");
    }

    [Test]
    public async Task Parse_String_Valid_ReturnsInstance()
    {
        var v = SemVersion.Parse("2.0.1-beta");
        await Assert.That(v.Major).IsEqualTo((ushort)2);
        await Assert.That(v.Minor).IsEqualTo((ushort)0);
        await Assert.That(v.Patch).IsEqualTo((ushort)1);
        await Assert.That(v.Prerelease).IsEqualTo("beta");
    }

    [Test]
    public async Task Parse_Span_Valid_ReturnsInstance()
    {
        ReadOnlySpan<char> s = "3.4.5+meta".AsSpan();
        var v = SemVersion.Parse(s);
        await Assert.That(v.Major).IsEqualTo((ushort)3);
        await Assert.That(v.Minor).IsEqualTo((ushort)4);
        await Assert.That(v.Patch).IsEqualTo((ushort)5);
        await Assert.That(v.Build).IsEqualTo("meta");
    }

    // ---------------------------
    // Parse / TryParse - invalid inputs
    // ---------------------------

    [Test]
    public async Task TryParse_NullOrEmpty_False()
    {
        await Assert.That(SemVersion.TryParse(null, out _)).IsFalse();
        await Assert.That(SemVersion.TryParse("", out _)).IsFalse();
    }

    [Test]
    public async Task TryParse_OnlyDashOrPlus_False()
    {
        await Assert.That(SemVersion.TryParse("-", out _)).IsFalse();
        await Assert.That(SemVersion.TryParse("+", out _)).IsFalse();
    }

    [Test]
    public async Task TryParse_DashWithNothingAfter_False()
    {
        await Assert.That(SemVersion.TryParse("1.2.3-", out _)).IsFalse();
    }

    [Test]
    public async Task TryParse_PlusWithNothingAfter_False()
    {
        await Assert.That(SemVersion.TryParse("1.2.3+", out _)).IsFalse();
    }

    [Test]
    public async Task TryParse_TooManyDots_False()
    {
        await Assert.That(SemVersion.TryParse("1.2.3.4", out _)).IsFalse();
    }

    [Test]
    public async Task TryParse_EmptySegments_False()
    {
        await Assert.That(SemVersion.TryParse("1..2", out _)).IsFalse();
        await Assert.That(SemVersion.TryParse(".1.2", out _)).IsFalse();
        await Assert.That(SemVersion.TryParse("1.", out _)).IsFalse();
        await Assert.That(SemVersion.TryParse(".1", out _)).IsFalse();
    }

    [Test]
    public async Task TryParse_NonNumericCore_False()
    {
        await Assert.That(SemVersion.TryParse("v1.2.3", out _)).IsFalse();
        await Assert.That(SemVersion.TryParse("1.x.3", out _)).IsFalse();
    }

    [Test]
    public async Task Parse_Invalid_ThrowsFormatException()
    {
        await Assert
            .That(() => SemVersion.Parse("not-a-version"))
            .ThrowsExactly<FormatException>();
    }

    [Test]
    public async Task TryParse_ExceedsMaxLength_False()
    {
        // Build a version that exceeds MaxLength after trim
        var sb = new StringBuilder();
        sb.Append("1.2.3-");
        sb.Append(new string('a', SemVersion.MaxLength)); // force over limit
        var longVersion = sb.ToString();

        await Assert.That(SemVersion.TryParse(longVersion, out _)).IsFalse();
    }
    
    [Test]
    public async Task TryParse_LeadingWhitespace_Fails()
    {
        await Assert.That(SemVersion.TryParse(" 1.2.3", out _)).IsFalse();
    }

    [Test]
    public async Task TryParse_TrailingWhitespace_Fails()
    {
        await Assert.That(SemVersion.TryParse("1.2.3 ", out _)).IsFalse();
    }

    [Test]
    public async Task TryParse_OnlyWhitespace_Fails()
    {
        await Assert.That(SemVersion.TryParse("   ", out _)).IsFalse();
    }

    // ---------------------------
    // ToString
    // ---------------------------

    [Test]
    public async Task ToString_CoreOnly_ThreeParts()
    {
        var v = new SemVersion(1, 2, 3);
        await Assert.That(v.ToString()).IsEqualTo("1.2.3");
    }

    [Test]
    public async Task ToString_WithPrerelease()
    {
        var v = new SemVersion(1, 0, 0, "alpha");
        await Assert.That(v.ToString()).IsEqualTo("1.0.0-alpha");
    }

    [Test]
    public async Task ToString_WithBuild()
    {
        var v = new SemVersion(1, 0, 0, build: "001");
        await Assert.That(v.ToString()).IsEqualTo("1.0.0+001");
    }

    [Test]
    public async Task ToString_WithPrereleaseAndBuild()
    {
        var v = new SemVersion(1, 2, 3, "rc.1", "exp.sha");
        await Assert.That(v.ToString()).IsEqualTo("1.2.3-rc.1+exp.sha");
    }

    // ---------------------------
    // Equality / GetHashCode
    // ---------------------------

    [Test]
    public async Task Equals_Structural_IncludingBuild()
    {
        var a = new SemVersion(1, 2, 3, "alpha", "b1");
        var b = new SemVersion(1, 2, 3, "alpha", "b1");

        await Assert.That(a.Equals(b)).IsTrue();
        await Assert.That(a.Equals((object)b)).IsTrue();
        await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
    }

    [Test]
    public async Task Equals_DiffersInBuild_False()
    {
        var a = new SemVersion(1, 2, 3, "alpha", "b1");
        var b = new SemVersion(1, 2, 3, "alpha", "b2");

        await Assert.That(a.Equals(b)).IsFalse();
    }

    [Test]
    public async Task Equals_DiffersInPrerelease_False()
    {
        var a = new SemVersion(1, 2, 3, "alpha");
        var b = new SemVersion(1, 2, 3, "beta");

        await Assert.That(a.Equals(b)).IsFalse();
    }

    [Test]
    public async Task Equals_Null_False()
    {
        var a = new SemVersion(0, 0, 0);
        await Assert.That(a.Equals(null)).IsFalse();
        await Assert.That(a!.Equals((object?)null)).IsFalse();
    }

    // ---------------------------
    // FBS roundtrip (requires your Serialization.Types.SemVer)
    // ---------------------------

    [Test]
    public async Task Fbs_Roundtrip_PreservesFields()
    {
        var original = new SemVersion(10, 20, 30, "alpha.1", "build.7");
        var fbs = original.ToFbs();
        var roundtrip = SemVersion.FromFbs(fbs);

        await Assert.That(roundtrip.Major).IsEqualTo(original.Major);
        await Assert.That(roundtrip.Minor).IsEqualTo(original.Minor);
        await Assert.That(roundtrip.Patch).IsEqualTo(original.Patch);
        await Assert.That(roundtrip.Prerelease).IsEqualTo(original.Prerelease);
        await Assert.That(roundtrip.Build).IsEqualTo(original.Build);
    }

    // ---------------------------
    // Span overloads (extra coverage)
    // ---------------------------

    [Test]
    public async Task TryParse_Span_Input_Succeeds()
    {
        ReadOnlySpan<char> s = "7.8.9-beta+ci".AsSpan();
        var ok = SemVersion.TryParse(s, out var v);

        await Assert.That(ok).IsTrue();
        await Assert.That(v!.Major).IsEqualTo((ushort)7);
        await Assert.That(v.Minor).IsEqualTo((ushort)8);
        await Assert.That(v.Patch).IsEqualTo((ushort)9);
        await Assert.That(v.Prerelease).IsEqualTo("beta");
        await Assert.That(v.Build).IsEqualTo("ci");
    }
}
