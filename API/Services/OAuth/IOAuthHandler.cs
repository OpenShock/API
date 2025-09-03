using OneOf;

namespace OpenShock.API.Services.OAuth;

public sealed record ExternalUser(
    string Provider,          // "discord", "github", etc.
    string ExternalId,        // provider user id
    string? Username,
    string? Email,            // provider email
    string? AvatarUrl);

public sealed record OAuthStartContext(string? ReturnTo, OAuthFlow Flow);
public sealed record OAuthCallbackResult(ExternalUser User);

public sealed record OAuthErrorResult(string Code, string Description);

public interface IOAuthHandler
{
    /// A short, case-insensitive key (e.g., "discord").
    string Key { get; }

    /// Build the provider authorize URL and set any cookies you need (state, pkce, return_to).
    Task<OneOf<string, OAuthErrorResult>> BuildAuthorizeUrlAsync(HttpContext http, OAuthStartContext ctx);

    /// Handle callback: validate state, exchange code, fetch user, clear cookies, etc.
    Task<OneOf<OAuthCallbackResult, OAuthErrorResult>> HandleCallbackAsync(HttpContext http, IQueryCollection query);
}