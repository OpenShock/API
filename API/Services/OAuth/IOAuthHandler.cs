using OneOf;

namespace OpenShock.API.Services.OAuth;

public sealed record ExternalUser(
    string Provider,          // "discord", "github", etc.
    string ExternalId,        // provider user id
    string? Username,
    string? Email,            // provider email
    string? AvatarUrl);

public sealed record OAuthCallbackResult(ExternalUser User, string CallbackUrl);

public sealed record OAuthErrorResult(string Code, string Description);

public interface IOAuthHandler
{
    string Key { get; }
    string AuthorizeEndpoint { get; }
    string TokenEndpoint { get; }
    string UserInfoEndpoint { get; }

    /// Build the provider authorize URL
    Uri BuildAuthorizeUrl(string state, Uri callbackUrl);

    /// Handle callback: validate state, exchange code, fetch user, clear cookies, etc.
    Task<OneOf<OAuthCallbackResult, OAuthErrorResult>> HandleCallbackAsync(HttpContext http, IQueryCollection query, Uri callbackUrl);
}