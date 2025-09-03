using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using OneOf;
using OpenShock.Common.Utils;
using System.Net.Http.Headers;
using System.Text.Json;

namespace OpenShock.API.Services.OAuth.Discord;

public sealed class DiscordOAuthHandler : IOAuthHandler
{
    private const string AuthorizeEndpoint = "https://discord.com/oauth2/authorize";
    private const string TokenEndpoint = "https://discord.com/api/oauth2/token";
    private const string UserInfoEndpoint = "https://discord.com/api/users/@me";

    private const string CallbackPath = "/1/account/oauth/callback/discord";

    private readonly IHttpClientFactory _http;
    private readonly DiscordOAuthOptions _opt;
    private readonly IOAuthStateStore _stateStore;

    public DiscordOAuthHandler(
        IHttpClientFactory http,
        IOptions<DiscordOAuthOptions> opt,
        IOAuthStateStore stateStore)
    {
        _http = http;
        _opt = opt.Value;
        _stateStore = stateStore;
    }

    public string Key => "discord";

    public async Task<OneOf<string, OAuthErrorResult>> BuildAuthorizeUrlAsync(HttpContext http, OAuthStartContext ctx)
    {
        if (string.IsNullOrWhiteSpace(_opt.ClientId))
            return new OAuthErrorResult("config_error", "Discord OAuth is not configured.");

        var callback = BuildCallbackUrl();
        if (callback is null)
            return new OAuthErrorResult("config_error", "Callback base URL is not configured.");

        // Opaque nonce for state
        var nonce = CryptoUtils.RandomString(64);

        // Save full envelope in Redis with TTL
        var env = new OAuthStateEnvelope(
            Provider: Key,
            State: nonce,
            Flow: ctx.Flow,
            ReturnTo: ctx.ReturnTo,
            UserId: null,            // set if you add an authenticated “link” endpoint
            CodeVerifier: null,      // add PKCE later if desired
            CreatedAt: DateTimeOffset.UtcNow
        );

        // 10 minutes is plenty
        await _stateStore.SaveAsync(http, env, TimeSpan.FromMinutes(10));

        // Build Discord authorize URL
        var qb = new QueryBuilder
        {
            { "response_type", "code" },
            { "client_id",  _opt.ClientId },
            { "scope",      "identify email" },
            { "redirect_uri", callback },
            { "state",      nonce }
        };

        var url = new UriBuilder(AuthorizeEndpoint) { Query = qb.ToString() }.Uri.ToString();
        return url;
    }

    public async Task<OneOf<OAuthCallbackResult, OAuthErrorResult>> HandleCallbackAsync(HttpContext http, IQueryCollection query)
    {
        if (string.IsNullOrWhiteSpace(_opt.ClientId) || string.IsNullOrWhiteSpace(_opt.ClientSecret))
            return new OAuthErrorResult("config_error", "Discord OAuth is not configured.");

        var code = query["code"].ToString();
        var state = query["state"].ToString();

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return new OAuthErrorResult("invalid_request", "Missing 'code' or 'state'.");

        var env = await _stateStore.ReadAndClearAsync(http, Key, state);
        if (env is null)
            return new OAuthErrorResult("state_invalid", "Invalid or expired state.");

        var callback = BuildCallbackUrl();
        if (callback is null)
            return new OAuthErrorResult("config_error", "Callback base URL is not configured.");

        var ct = http.RequestAborted;
        var client = _http.CreateClient();

        // Exchange code for token
        var accessResult = await ExchangeCodeForAccessTokenAsync(client, code, callback, ct);
        if (accessResult.TryPickT1(out var tokenErr, out var accessToken))
            return tokenErr;

        // Fetch user info
        var userResult = await FetchDiscordUserAsync(client, accessToken, ct);
        if (userResult.TryPickT1(out var userErr, out var me))
            return userErr;

        var externalId = me.GetProperty("id").GetString()!;
        var username = me.GetProperty("username").GetString();
        string? email = me.TryGetProperty("email", out var emailEl) ? emailEl.GetString() : null;

        var user = new ExternalUser(
            Provider: Key,
            ExternalId: externalId,
            Username: username,
            Email: email,
            AvatarUrl: null
        );

        http.Items["oauth_flow"] = env.Flow;

        return new OAuthCallbackResult(user);
    }

    // ------------------
    // Helper methods
    // ------------------

    private string? BuildCallbackUrl()
    {
        try
        {
            return new Uri(new Uri("https://api.openhshock.dev"), CallbackPath).ToString();
        }
        catch
        {
            return null;
        }
    }

    private async Task<OneOf<string, OAuthErrorResult>> ExchangeCodeForAccessTokenAsync(
        HttpClient client,
        string code,
        string callback,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _opt.ClientId,
                ["client_secret"] = _opt.ClientSecret,
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = callback
            })
        };
        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return new OAuthErrorResult("token_exchange_failed", $"Token exchange failed ({(int)response.StatusCode}).");

        var tokenEl = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        if (!tokenEl.TryGetProperty("access_token", out var accessEl) ||
            string.IsNullOrWhiteSpace(accessEl.GetString()))
            return new OAuthErrorResult("token_exchange_failed", "No access token from provider.");

        return accessEl.GetString()!;
    }

    private async Task<OneOf<JsonElement, OAuthErrorResult>> FetchDiscordUserAsync(
        HttpClient client,
        string accessToken,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, UserInfoEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return new OAuthErrorResult("profile_fetch_failed", $"Failed to fetch user profile ({(int)response.StatusCode}).");

        return await response.Content.ReadFromJsonAsync<JsonElement>(ct);
    }
}
