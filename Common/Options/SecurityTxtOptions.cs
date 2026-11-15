using System.Globalization;
using System.Text;

namespace OpenShock.Common.Options;

/// <summary>
/// Contents of the <c>/.well-known/security.txt</c> file (RFC 9116) this host publishes, so that security
/// researchers know where to report a vulnerability in <em>this deployment</em>. There is deliberately no
/// default: a self-hosted instance must not advertise somebody else's security contact.
/// </summary>
public sealed class SecurityTxtOptions
{
    public const string SectionName = "OpenShock:SecurityTxt";
    public const string Path = "/.well-known/security.txt";

    /// <summary>Where to report, in order of preference. Each entry is a <c>mailto:</c> or <c>https:</c> URI.</summary>
    public required IReadOnlyList<Uri> Contact { get; init; }

    /// <summary>When the file stops being valid. RFC 9116 recommends less than a year ahead.</summary>
    public required DateTimeOffset Expires { get; init; }

    public Uri? Policy { get; init; }
    public Uri? Acknowledgments { get; init; }

    /// <summary>Comma separated language tags, e.g. <c>en, de</c>.</summary>
    public string? PreferredLanguages { get; init; }

    public bool IsExpired(DateTimeOffset now) => now >= Expires;

    /// <summary>
    /// Reads the section, or returns <c>null</c> when no contact is configured, which disables the file.
    /// Anything half-configured throws, so a typo fails startup instead of silently publishing nothing.
    /// </summary>
    public static SecurityTxtOptions? FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);

        var contacts = (section["Contact"] ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => ParseUri(value, "Contact", allowMailto: true))
            .ToArray();
        if (contacts.Length == 0) return null;

        var expiresRaw = section["Expires"];
        if (string.IsNullOrWhiteSpace(expiresRaw))
            throw new InvalidOperationException($"{SectionName}:Expires is required when {SectionName}:Contact is set.");
        if (!DateTimeOffset.TryParse(expiresRaw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var expires))
            throw new InvalidOperationException($"{SectionName}:Expires is not a valid date: '{expiresRaw}'. Use e.g. 2027-10-31T00:00:00Z.");

        var languages = section["PreferredLanguages"]?.Trim();
        if (languages is not null && languages.AsSpan().ContainsAny('\r', '\n'))
            throw new InvalidOperationException($"{SectionName}:PreferredLanguages must be a single line.");

        return new SecurityTxtOptions
        {
            Contact = contacts,
            Expires = expires,
            Policy = ParseOptionalUri(section["Policy"], "Policy"),
            Acknowledgments = ParseOptionalUri(section["Acknowledgments"], "Acknowledgments"),
            PreferredLanguages = string.IsNullOrEmpty(languages) ? null : languages,
        };
    }

    /// <param name="canonical">The URL this file is being served from, or <c>null</c> to leave the field out.</param>
    public string Render(Uri? canonical = null)
    {
        var sb = new StringBuilder();

        foreach (var contact in Contact) sb.Append("Contact: ").Append(contact.AbsoluteUri).Append('\n');
        sb.Append("Expires: ").Append(Expires.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)).Append('\n');
        if (PreferredLanguages is not null) sb.Append("Preferred-Languages: ").Append(PreferredLanguages).Append('\n');
        if (canonical is not null) sb.Append("Canonical: ").Append(canonical.AbsoluteUri).Append('\n');
        if (Policy is not null) sb.Append("Policy: ").Append(Policy.AbsoluteUri).Append('\n');
        if (Acknowledgments is not null) sb.Append("Acknowledgments: ").Append(Acknowledgments.AbsoluteUri).Append('\n');

        return sb.ToString();
    }

    private static Uri? ParseOptionalUri(string? value, string name) =>
        string.IsNullOrWhiteSpace(value) ? null : ParseUri(value.Trim(), name, allowMailto: false);

    // RFC 9116 requires web URIs to be https. Uri.AbsoluteUri percent-encodes control characters, so a
    // value can never smuggle an extra line into the file.
    private static Uri ParseUri(string value, string name, bool allowMailto)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || (allowMailto && uri.Scheme == Uri.UriSchemeMailto)))
        {
            return uri;
        }

        throw new InvalidOperationException(
            $"{SectionName}:{name} must be {(allowMailto ? "a mailto: or https: URI" : "an https: URL")}, got '{value}'.");
    }
}
