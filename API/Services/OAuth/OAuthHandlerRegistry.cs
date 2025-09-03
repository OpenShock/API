using OneOf;
using OpenShock.Common.Utils;

namespace OpenShock.API.Services.OAuth;

public sealed class OAuthHandlerRegistry : IOAuthHandlerRegistry
{
    private readonly Dictionary<string, IOAuthHandler> _handlers;
    private readonly IOAuthStateStore _state;

    public OAuthHandlerRegistry(IEnumerable<IOAuthHandler> handlers, IOAuthStateStore state)
    {
        _handlers = handlers.ToDictionary(h => h.Key, h => h, StringComparer.OrdinalIgnoreCase);
        _state = state;
    }

    public string[] ListProviderKeys()
    {
        return _handlers.Keys.ToArray();
    }

    private Uri GetCallbackUri(string provider)
    {
        return new Uri($"https://api.openshock.app/1/oauth/{provider}/callback");
    }

    public async Task<OneOf<Uri, OAuthErrorResult, OAuthProviderNotSupported>> StartAuthorizeAsync(HttpContext http, string provider, OAuthFlow flow, string returnTo)
    {
        if (!_handlers.TryGetValue(provider, out var handler))
            return new OAuthProviderNotSupported();

        // Generate state and persist in Redis (+ double-submit cookie inside store)
        var stateNonce = CryptoUtils.RandomString(64);
        var env = new OAuthStateEnvelope(
            Provider: handler.Key,
            State: stateNonce,
            Flow: flow,
            ReturnTo: returnTo,
            UserId: null,
            CreatedAt: DateTimeOffset.UtcNow
        );

        await _state.SaveAsync(http, env, TimeSpan.FromMinutes(10));

        // Delegate URL construction to the handler (includes redirect_uri & scopes)
        return handler.BuildAuthorizeUrl(stateNonce, GetCallbackUri(provider));
    }

    public async Task<OneOf<OAuthCallbackResult, OAuthErrorResult, OAuthProviderNotSupported>> HandleCallbackAsync(HttpContext http, string provider, IQueryCollection query)
    {
        if (!_handlers.TryGetValue(provider, out var handler))
            return new OAuthProviderNotSupported();

        var result = await handler.HandleCallbackAsync(http, query, GetCallbackUri(provider));
        if (result.TryPickT1(out var error, out var info))
        {
            return error;
        }

        return info;
    }
}