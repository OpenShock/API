using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using OpenShock.API.Options.OAuth;
using OpenShock.Common.Authentication;
using OpenShock.Common.Errors;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    private const string DiscordApiBase = "https://api.openshock.app"; // TODO: move to config
    private const string DefaultReturn = "https://app.openshock.app/auth/callback/discord"; // TODO: move to config

    // Very small DTOs for Discord responses
    private sealed class DiscordTokenResponse
    {
        public string access_token { get; set; } = default!;
        public string token_type   { get; set; } = default!;
        public int    expires_in   { get; set; }
        public string? refresh_token { get; set; }
        public string? scope         { get; set; }
    }

    private sealed class DiscordUser
    {
        public string id            { get; set; } = default!;
        public string username      { get; set; } = default!;
        public string discriminator { get; set; } = "0";
        public string? global_name  { get; set; }
        public string? avatar       { get; set; }
    }

    [EnableRateLimiting("auth")]
    [HttpGet("oauth/callback/{provider}")]
    [EnableCors("allow_sso_providers")]
    public async Task<IActionResult> OAuthAuthenticate(
        [FromRoute] string provider,
        [FromServices] IHttpClientFactory httpClientFactory,
        [FromServices] IOptions<DiscordOAuthOptions> discordOptionsSnap)
    {
        if (!OpenShockAuthSchemes.OAuth2Schemes.Contains(provider))
            return Problem(OAuthError.ProviderNotSupported);

        if (!string.Equals(provider, "discord", StringComparison.OrdinalIgnoreCase)) // temporary
            return Problem(OAuthError.ProviderNotSupported);

        // Read query values dynamically (only code & state are expected for Discord)
        var code  = Request.Query["code"].ToString();
        var state = Request.Query["state"].ToString();

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return BadRequest("Missing 'code' or 'state'.");

        // Read & clear state cookie
        if (!Request.Cookies.TryGetValue(StateCookieName, out var cookieVal))
            return BadRequest("Missing state cookie.");

        // cookie format from /start: "<randomState>|<return_to>"
        string cookieState;
        string? rawReturnTo = null;
        var pipeIdx = cookieVal.IndexOf('|');
        if (pipeIdx >= 0)
        {
            cookieState = cookieVal[..pipeIdx];
            rawReturnTo = cookieVal[(pipeIdx + 1)..];
            if (string.IsNullOrWhiteSpace(rawReturnTo)) rawReturnTo = null;
        }
        else
        {
            cookieState = cookieVal;
        }

        Response.Cookies.Delete(StateCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });

        if (!string.Equals(state, cookieState, StringComparison.Ordinal))
            return BadRequest("Invalid state.");

        // Exchange authorization code for tokens
        var discordOptions = discordOptionsSnap.Value;
        var callbackUri = new Uri(new Uri(DiscordApiBase), "/1/account/oauth/callback/discord");

        DiscordTokenResponse? token;
        var client = httpClientFactory.CreateClient();
        using (var tokenReq = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token"))
        {
            tokenReq.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = discordOptions.ClientId,
                ["client_secret"] = discordOptions.ClientSecret,
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = callbackUri.ToString()
            });

            using var tokenRes = await client.SendAsync(tokenReq);
            if (!tokenRes.IsSuccessStatusCode)
                return BadRequest($"Token exchange failed ({(int)tokenRes.StatusCode}).");

            token = JsonSerializer.Deserialize<DiscordTokenResponse>(
                await tokenRes.Content.ReadAsStringAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        if (token?.access_token is null)
            return BadRequest("No access token from provider.");

        // Fetch Discord user
        using var meReq = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me");
        meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.access_token);
        using var meRes = await client.SendAsync(meReq);
        if (!meRes.IsSuccessStatusCode)
            return BadRequest($"Failed to fetch user profile ({(int)meRes.StatusCode}).");

        var user = JsonSerializer.Deserialize<DiscordUser>(
            await meRes.Content.ReadAsStringAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (user is null)
            return BadRequest("Invalid user payload from provider.");

        // TODO: Link/auth the account (by user.id), create session/JWT, set your own auth cookie/header here.

        // Where to redirect next (keep whitelisting off until you add it)

        // If/when you add whitelisting:
        // if (Uri.TryCreate(rawReturnTo, UriKind.Absolute, out var rt) && IsAllowedReturnUrl(rt, discordOptions.AllowedReturnHosts))
        //     redirectTarget = rt.ToString();

        return Redirect(DefaultReturn);
    }

    // If/when you enable return_to, keep a tiny allow-list like:
    /*
    private static bool IsAllowedReturnUrl(Uri url, IEnumerable<string> allowedHosts)
    {
        if (!string.Equals(url.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return false;

        var host = url.Host;
        foreach (var allowed in allowedHosts)
        {
            if (string.Equals(host, allowed, StringComparison.OrdinalIgnoreCase))
                return true;
            if (host.EndsWith("." + allowed, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
    */
}