namespace OpenShock.Common.Services.Turnstile;

public sealed class CloudflareTurnstileOptions
{
    public required string SiteKey { get; set; }
    public required string SecretKey { get; set; }
}