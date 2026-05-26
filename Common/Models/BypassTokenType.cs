namespace OpenShock.Common.Models;

[Flags]
public enum BypassTokenType
{
    None = 0,
    Turnstile = 1 << 0,
    RateLimit = 1 << 1
}
