using OneOf;

namespace OpenShock.API.Services.OAuth;

public interface IOAuthHandlerRegistry
{
    string[] ListProviderKeys();
    Task<OneOf<Uri, OAuthErrorResult, OAuthProviderNotSupported>> StartAuthorizeAsync(HttpContext http, string provider, OAuthFlow flow, string returnTo);
    Task<OneOf<OAuthCallbackResult, OAuthErrorResult, OAuthProviderNotSupported>> HandleCallbackAsync(HttpContext http, string provider, IQueryCollection query);
}

public readonly record struct OAuthProviderNotSupported;