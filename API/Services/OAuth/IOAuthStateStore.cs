namespace OpenShock.API.Services.OAuth;

public interface IOAuthStateStore
{
    Task SaveAsync(HttpContext http, OAuthStateEnvelope envelope, TimeSpan ttl);
    Task<OAuthStateEnvelope?> ReadAndClearAsync(HttpContext http, string provider, string state);
}

public enum OAuthFlow
{
    SignIn,
    Link
}
public sealed record OAuthStateEnvelope(
    string Provider,
    string State,                  // opaque nonce
    OAuthFlow Flow,                // SignIn | Link
    string? ReturnTo,              // optional allow-listed redirect
    Guid? UserId,                  // set for Link flow
    string? CodeVerifier,          // if using PKCE
    DateTimeOffset CreatedAt
);