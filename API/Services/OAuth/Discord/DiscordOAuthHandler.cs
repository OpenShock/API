using OneOf;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using OpenShock.Common.Utils;

namespace OpenShock.API.Services.OAuth.Discord;

public sealed class DiscordOAuthHandler : IOAuthHandler
{
    private const string AuthorizeEndpoint = "https://discord.com/oauth2/authorize";
    private const string TokenEndpoint = "https://discord.com/api/oauth2/token";
    private const string UserInfoEndpoint = "https://discord.com/api/users/@me";
    
    private const string CallbackPath ="/1/account/oauth/callback/discord";
    
    private readonly IHttpClientFactory _http;
    private readonly IOptions<DiscordOAuthOptions> _opt;
    private readonly IOAuthStateStore _state;

    public DiscordOAuthHandler(IHttpClientFactory http, IOptions<DiscordOAuthOptions> opt, IOAuthStateStore state)
    {
        _http = http; _opt = opt; _state = state;
    }

    public string Key => "discord";

    public OneOf<string, OAuthErrorResult> BuildAuthorizeUrl(HttpContext http, OAuthStartContext ctx)
    {
        var o = _opt.Value;
        var callback = new Uri(new Uri(o.CallbackBase.TrimEnd('/')), CallbackPath).ToString();

        var state = CryptoUtils.RandomString(64);
        _state.Save(http, Key, state, ctx.ReturnTo);

        var qb = new QueryBuilder
        {
            { "response_type", "code" },
            { "client_id",  o.ClientId },
            { "scope",      "identify" },
            { "redirect_uri", callback },
            { "state",      state }
        };
        return new UriBuilder(AuthorizeEndpoint) { Query = qb.ToString() }.Uri.ToString();
    }

    public async Task<OneOf<OAuthCallbackResult, OAuthErrorResult>> HandleCallbackAsync(HttpContext http, IQueryCollection query)
    {
        var o = _opt.Value;

        var code  = query["code"].ToString();
        var state = query["state"].ToString();
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            throw new InvalidOperationException("Missing code/state");

        var saved = _state.ReadAndClear(http, Key);
        if (saved is null || !string.Equals(saved.Value.State, state, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid state");

        var callback = new Uri(new Uri(o.CallbackBase.TrimEnd('/')), CallbackPath).ToString();

        var client = _http.CreateClient();
        using var tokenReq = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string,string>
            {
                ["client_id"] = o.ClientId,
                ["client_secret"] = o.ClientSecret,
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = callback
            })
        };
        using var tokenRes = await client.SendAsync(tokenReq);
        tokenRes.EnsureSuccessStatusCode();

        var token = JsonSerializer.Deserialize<JsonElement>(await tokenRes.Content.ReadAsStringAsync());
        var access = token.GetProperty("access_token").GetString()!;

        using var meReq = new HttpRequestMessage(HttpMethod.Get, UserInfoEndpoint);
        meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
        using var meRes = await client.SendAsync(meReq);
        meRes.EnsureSuccessStatusCode();

        var me = JsonSerializer.Deserialize<JsonElement>(await meRes.Content.ReadAsStringAsync());
        var user = new ExternalUser(
            Provider: Key,
            ExternalId: me.GetProperty("id").GetString()!,
            Username: me.GetProperty("username").GetString(),
            DisplayName: me.TryGetProperty("global_name", out var gn) ? gn.GetString() : null,
            AvatarUrl: null // build if you need it
        );

        return new OAuthCallbackResult(user);
    }
}