namespace OpenShock.API.OAuth;

public sealed record OAuthSnapshot(
    string Provider,
    string ExternalId,
    string? Email,
    string? UserName,
    IDictionary<string, string> Tokens,
    DateTimeOffset IssuedUtc);