using OpenShock.Common.Utils;

namespace OpenShock.Common.Tests.Utils;

public class DomainUtilsTests
{
    [Test]
    public async Task NullString_ReturnsFalse()
    {
        // Act
        var result = DomainUtils.IsValidDomain(null);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task EmptyString_ReturnsFalse()
    {
        // Act
        var result = DomainUtils.IsValidDomain("");

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task NoDot_ReturnsFalse()
    {
        // Act
        var result = DomainUtils.IsValidDomain("example");

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task LeadingDot_ReturnsFalse()
    {
        // Act
        var result = DomainUtils.IsValidDomain(".example.com");

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task TrailingDot_ReturnsFalse()
    {
        // Act
        var result = DomainUtils.IsValidDomain("example.com.");

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task ConsecutiveDots_ReturnsFalse()
    {
        // Act
        var result = DomainUtils.IsValidDomain("a..b.com");

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task LabelTooLong_ReturnsFalse()
    {
        // Arrange: label with 64 chars (invalid), then ".com"
        var tooLong = new string('a', 64) + ".com";

        // Act
        var result = DomainUtils.IsValidDomain(tooLong);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task LabelStartingHyphen_ReturnsFalse()
    {
        // Act
        var result = DomainUtils.IsValidDomain("-abc.com");

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task LabelEndingHyphen_ReturnsFalse()
    {
        // Act
        var result = DomainUtils.IsValidDomain("abc-.com");

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task InvalidCharacters_ReturnsFalse()
    {
        // Act
        var r1 = DomainUtils.IsValidDomain("exa_mple.com");
        var r2 = DomainUtils.IsValidDomain("examp le.com");
        var r3 = DomainUtils.IsValidDomain("exam!ple.com");

        // Assert
        await Assert.That(r1).IsFalse();
        await Assert.That(r2).IsFalse();
        await Assert.That(r3).IsFalse();
    }

    [Test]
    public async Task TotalLengthOver253_ReturnsFalse()
    {
        // Arrange: construct >253 chars with dots
        // "a." repeated 200 times yields 400 chars; ensure clearly >253.
        var longHost = string.Join('.', Enumerable.Repeat("a", 130)); // 129 dots + 130 a's ~ 259 chars

        // Act
        var result = DomainUtils.IsValidDomain(longHost);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task ValidAsciiLDH_ReturnsTrue()
    {
        // Act
        var r1 = DomainUtils.IsValidDomain("example.com");
        var r2 = DomainUtils.IsValidDomain("a.b");
        var r3 = DomainUtils.IsValidDomain("foo-bar.baz0");
        var r4 = DomainUtils.IsValidDomain("xn--d1acufc.xn--p1ai"); // Punycode

        // Assert
        await Assert.That(r1).IsTrue();
        await Assert.That(r2).IsTrue();
        await Assert.That(r3).IsTrue();
        await Assert.That(r4).IsTrue();
    }

    [Test]
    public async Task HostMatchesCookieDomain_ExactMatch_ReturnsTrue()
    {
        // Act
        var result = DomainUtils.HostMatchesCookieDomain("example.com", "example.com");

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task HostMatchesCookieDomain_SuffixLabelMatch_ReturnsTrue()
    {
        // Act
        var result = DomainUtils.HostMatchesCookieDomain("shop.foo.example.com".AsSpan(), "example.com".AsSpan());

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task HostMatchesCookieDomain_PartialSuffixNoBoundary_ReturnsFalse()
    {
        // Act
        var result = DomainUtils.HostMatchesCookieDomain("badexample.com", "example.com");

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task HostMatchesCookieDomain_CookieLongerThanHost_ReturnsFalse()
    {
        // Act
        var result = DomainUtils.HostMatchesCookieDomain("example.com", "foo.example.com");

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task HostMatchesCookieDomain_InvalidHost_ReturnsFalse()
    {
        // Act
        var result = DomainUtils.HostMatchesCookieDomain("example", "example.com"); // host without dot is invalid

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task HostMatchesCookieDomain_CaseInsensitive_ReturnsTrue()
    {
        // Act
        var result = DomainUtils.HostMatchesCookieDomain("SHOP.Foo.Example.COM", "example.com");

        // Assert
        await Assert.That(result).IsTrue();
    }

    // ---- GetBestMatchingCookieDomain ----

    [Test]
    public async Task GetBestMatchingCookieDomain_PicksMostSpecific()
    {
        // Arrange
        string[] list = ["example.com","foo.example.com","bar.com"];

        // Act
        var best = DomainUtils.GetBestMatchingCookieDomain("shop.foo.example.com", list);

        // Assert
        await Assert.That(best).IsEqualTo("foo.example.com");
    }

    [Test]
    public async Task GetBestMatchingCookieDomain_ExactMatchBeatsShorterSuffix()
    {
        // Arrange
        string[] list = ["example.com","shop.foo.example.com"];

        // Act
        var best = DomainUtils.GetBestMatchingCookieDomain("shop.foo.example.com", list);

        // Assert
        await Assert.That(best).IsEqualTo("shop.foo.example.com");
    }

    [Test]
    public async Task GetBestMatchingCookieDomain_EmptySegmentsIgnored()
    {
        // Arrange
        string[] list = ["","","example.com","","foo.example.com",""];

        // Act
        var best = DomainUtils.GetBestMatchingCookieDomain("shop.foo.example.com", list);

        // Assert
        await Assert.That(best).IsEqualTo("foo.example.com");
    }

    [Test]
    public async Task GetBestMatchingCookieDomain_InvalidDomainsIgnored()
    {
        // Arrange: includes invalid ".example.com" and "exa_mple.com"
        string[] list = [".example.com","exa_mple.com","example.com"];

        // Act
        var best = DomainUtils.GetBestMatchingCookieDomain("shop.example.com", list);

        // Assert
        await Assert.That(best).IsEqualTo("example.com");
    }

    [Test]
    public async Task GetBestMatchingCookieDomain_NoMatch_ReturnsNull()
    {
        // Arrange
        string[] list = ["foo.com","bar.net"];

        // Act
        var best = DomainUtils.GetBestMatchingCookieDomain("example.com", list);

        // Assert
        await Assert.That(best).IsNull();
    }

    [Test]
    public async Task GetBestMatchingCookieDomain_HostInvalid_ReturnsNull()
    {
        // Arrange
        string[] list = ["example.com","foo.example.com"];

        // Act
        var best = DomainUtils.GetBestMatchingCookieDomain("example", list);

        // Assert
        await Assert.That(best).IsNull();
    }

    [Test]
    public async Task GetBestMatchingCookieDomain_WhitespaceNotTrimmedInCurrentImplementation_SkipsEntry()
    {
        // Arrange: first entry has spaces (invalid for current impl), second is valid and more specific.
        string[] list = [" example.com ","foo.example.com"];

        // Use a host that matches *both* example.com and foo.example.com
        var best = DomainUtils.GetBestMatchingCookieDomain("shop.foo.example.com", list);

        // Assert: because the first is invalid (whitespace not trimmed), the best is foo.example.com
        await Assert.That(best).IsEqualTo("foo.example.com");
    }

    // ---- Additional boundary checks via HostMatchesCookieDomain ----

    [Test]
    public async Task HostBoundary_ChecksLabelBoundary()
    {
        // "ample.com" is substring but not a label-suffix of "example.com"
        var r1 = DomainUtils.HostMatchesCookieDomain("example.com", "ample.com");
        var r2 = DomainUtils.HostMatchesCookieDomain("xample.com", "ample.com");
        var r3 = DomainUtils.HostMatchesCookieDomain("fooample.com", "ample.com");

        await Assert.That(r1).IsFalse();
        await Assert.That(r2).IsFalse();
        await Assert.That(r3).IsFalse();
    }
}
