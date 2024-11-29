namespace OpenShock.Common.Services.Turnstile;

public sealed class CloudflareTurnstileOptions
{
    public required bool Enabled { get; set; }
    public required string SiteKey { get; set; }
    public required string SecretKey { get; set; }
}