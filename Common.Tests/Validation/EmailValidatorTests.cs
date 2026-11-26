using OpenShock.Common.Validation;

namespace OpenShock.Common.Tests.Validation;

internal sealed class EmailValidatorTests
{
    [Test]
    [Arguments("user@example.com")]
    [Arguments("User.Name+tag@Sub.Example.COM")]
    [Arguments("a!#$%&'*+-/=?^_`{|}~b@example.com")]
    [Arguments("x@a-b.c0")]
    [Arguments("taro..yamada@docomo.ne.jp")]    // dot placement strict dot-atom forbids, but carriers issued
    [Arguments("taro.@docomo.ne.jp")]
    [Arguments(".taro@ezweb.ne.jp")]
    public async Task IsValidAddress_Accepts(string address)
    {
        await Assert.That(EmailValidator.IsValidAddress(address)).IsTrue();
    }

    [Test]
    [Arguments("")]
    [Arguments("a@b")]                          // too short, and a single label domain
    [Arguments("user")]                         // no @
    [Arguments("@example.com")]                 // empty local part
    [Arguments("user@")]                        // empty domain
    [Arguments("a@b@example.com")]              // second @
    [Arguments(" user@example.com")]            // surrounding whitespace
    [Arguments("user@example.com ")]
    [Arguments("John <user@example.com>")]      // display name, which MailAddress accepts
    [Arguments("\"quoted\"@example.com")]       // quoted local part
    [Arguments("us er@example.com")]
    [Arguments("user@localhost")]               // single label
    [Arguments("user@1.2.3.4")]                 // IPv4 literal
    [Arguments("user@[1.2.3.4]")]
    [Arguments("user@-example.com")]            // hyphen placement
    [Arguments("user@example-.com")]
    [Arguments("user@exa_mple.com")]            // underscore
    [Arguments("user@example..com")]
    [Arguments("user@example.com.")]            // trailing dot
    [Arguments("user@münchen.de")]              // non-ASCII domain
    [Arguments("üser@example.com")]             // non-ASCII local part
    public async Task IsValidAddress_Rejects(string address)
    {
        await Assert.That(EmailValidator.IsValidAddress(address)).IsFalse();
    }

    [Test]
    public async Task IsValidAddress_EnforcesPartLengths()
    {
        await Assert.That(EmailValidator.IsValidAddress(new string('a', 64) + "@example.com")).IsTrue();
        await Assert.That(EmailValidator.IsValidAddress(new string('a', 65) + "@example.com")).IsFalse();
        await Assert.That(EmailValidator.IsValidAddress("user@" + new string('a', 63) + ".com")).IsTrue();
        await Assert.That(EmailValidator.IsValidAddress("user@" + new string('a', 64) + ".com")).IsFalse();
    }

    [Test]
    public async Task TrySplitAddress_ReturnsParts()
    {
        // Spans can't live across an await, so take the strings out first
        var ok = EmailValidator.TrySplitAddress("Some.One@Mail.Example.com", out var localSpan, out var domainSpan);
        var local = localSpan.ToString();
        var domain = domainSpan.ToString();

        await Assert.That(ok).IsTrue();
        await Assert.That(local).IsEqualTo("Some.One");
        await Assert.That(domain).IsEqualTo("Mail.Example.com");
    }

    [Test]
    public async Task TryNormalizeAddress_Lowercases()
    {
        await Assert.That(EmailValidator.TryNormalizeAddress("Some.One@Example.COM", out var normalized)).IsTrue();
        await Assert.That(normalized).IsEqualTo("some.one@example.com");

        await Assert.That(EmailValidator.TryNormalizeAddress("not an address", out _)).IsFalse();
    }

    [Test]
    [Arguments("example.com", "example.com")]
    [Arguments("  Example.COM  ", "example.com")]
    [Arguments("example.com.", "example.com")]
    [Arguments("someone@Example.com", "example.com")]
    [Arguments("weird local@example.com", "example.com")]   // only the domain of a pasted address matters
    public async Task TryNormalizeDomain_Accepts(string input, string expected)
    {
        await Assert.That(EmailValidator.TryNormalizeDomain(input, out var domain)).IsTrue();
        await Assert.That(domain).IsEqualTo(expected);
    }

    [Test]
    [Arguments("")]
    [Arguments("localhost")]
    [Arguments("1.2.3.4")]
    [Arguments("user@")]
    [Arguments("example.com..")]
    [Arguments("exa mple.com")]
    [Arguments("münchen.de")]
    public async Task TryNormalizeDomain_Rejects(string input)
    {
        await Assert.That(EmailValidator.TryNormalizeDomain(input, out _)).IsFalse();
    }
}
