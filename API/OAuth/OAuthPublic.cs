namespace OpenShock.API.OAuth;

// what we return to frontend at /oauth/discord/data
public sealed record OAuthPublic(
    string provider,
    string externalId,
    string? email,
    string? userName,
    string flowId,                 // opaque id the frontend will POST back to finalize
    int expiresInSeconds);