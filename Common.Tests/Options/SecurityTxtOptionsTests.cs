using Microsoft.Extensions.Configuration;
using OpenShock.Common.Options;

namespace OpenShock.Common.Tests.Options;

public sealed class SecurityTxtOptionsTests
{
    private static IConfiguration Config(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>($"{SecurityTxtOptions.SectionName}:{v.Key}", v.Value)))
            .Build();

    [Test]
    public async Task FromConfiguration_NoContact_IsDisabled()
    {
        await Assert.That(SecurityTxtOptions.FromConfiguration(Config())).IsNull();
        await Assert.That(SecurityTxtOptions.FromConfiguration(Config(("Contact", " , "), ("Expires", "2027-01-01")))).IsNull();
    }

    [Test]
    public async Task FromConfiguration_ContactWithoutExpires_Throws()
    {
        await Assert.That(() => SecurityTxtOptions.FromConfiguration(Config(("Contact", "mailto:security@example.org"))))
            .Throws<InvalidOperationException>();
    }

    [Test]
    [Arguments("Contact", "security@example.org")]
    [Arguments("Contact", "http://example.org/report")]
    [Arguments("Policy", "http://example.org/policy")]
    [Arguments("Policy", "mailto:security@example.org")]
    [Arguments("Expires", "next year")]
    public async Task FromConfiguration_InvalidValue_Throws(string key, string value)
    {
        var values = new Dictionary<string, string?>
        {
            ["Contact"] = "mailto:security@example.org",
            ["Expires"] = "2027-01-01T00:00:00Z",
            [key] = value,
        };

        await Assert.That(() => SecurityTxtOptions.FromConfiguration(Config(values.Select(kv => (kv.Key, kv.Value)).ToArray())))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Render_WritesAllFieldsInOrder()
    {
        var options = SecurityTxtOptions.FromConfiguration(Config(
            ("Contact", "mailto:security@example.org, https://example.org/report"),
            ("Expires", "2027-10-31"),
            ("Policy", "https://example.org/policy"),
            ("Acknowledgments", "https://example.org/thanks"),
            ("PreferredLanguages", "en, de")))!;

        var text = options.Render(new Uri("https://api.example.org/.well-known/security.txt"));

        await Assert.That(text).IsEqualTo(
            "Contact: mailto:security@example.org\n" +
            "Contact: https://example.org/report\n" +
            "Expires: 2027-10-31T00:00:00Z\n" +
            "Preferred-Languages: en, de\n" +
            "Canonical: https://api.example.org/.well-known/security.txt\n" +
            "Policy: https://example.org/policy\n" +
            "Acknowledgments: https://example.org/thanks\n");
    }

    [Test]
    public async Task Render_OmitsUnsetOptionalFields()
    {
        var options = SecurityTxtOptions.FromConfiguration(Config(
            ("Contact", "mailto:security@example.org"),
            ("Expires", "2027-10-31T12:00:00+02:00")))!;

        await Assert.That(options.Render()).IsEqualTo(
            "Contact: mailto:security@example.org\n" +
            "Expires: 2027-10-31T10:00:00Z\n");
    }

    [Test]
    public async Task Render_CannotBeInjectedWithExtraLines()
    {
        var options = SecurityTxtOptions.FromConfiguration(Config(
            ("Contact", "https://example.org/report\nContact: mailto:evil@example.org"),
            ("Expires", "2027-10-31")))!;

        await Assert.That(options.Render().Split('\n', StringSplitOptions.RemoveEmptyEntries).Length).IsEqualTo(2);
    }

    [Test]
    public async Task IsExpired_ComparesAgainstExpires()
    {
        var options = SecurityTxtOptions.FromConfiguration(Config(
            ("Contact", "mailto:security@example.org"),
            ("Expires", "2027-10-31T00:00:00Z")))!;

        await Assert.That(options.IsExpired(new DateTimeOffset(2027, 10, 30, 0, 0, 0, TimeSpan.Zero))).IsFalse();
        await Assert.That(options.IsExpired(new DateTimeOffset(2027, 10, 31, 0, 0, 0, TimeSpan.Zero))).IsTrue();
    }
}
