using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using OpenShock.API.Options.OAuth;
using OpenShock.Common.Authentication;
using OpenShock.Common.Errors;
using OpenShock.Common.Utils;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    // Cookie names
    private const string StateCookieName = "__openshock_oauth_state";

    [EnableRateLimiting("auth")]
    [HttpGet("oauth/start", Name = "InternalSsoAuthenticate")]
    public IActionResult OAuthAuthenticate(
        [FromQuery] string provider,
        [FromQuery(Name = "return_to")] Uri? returnTo,
        [FromServices] IOptions<DiscordOAuthOptions> discordOptions)
    {
        if (!OpenShockAuthSchemes.OAuth2Schemes.Contains(provider))
            return Problem(OAuthError.ProviderNotSupported);

        // Normalize provider
        provider = provider.ToLowerInvariant();
        if (provider is not "discord")
            return Problem(OAuthError.ProviderNotSupported); // TODO: KEEPME Temporary solution

        // Only Discord for now
        var options = discordOptions.Value;

        // Build your absolute callback (don’t hardcode)
        // e.g., options: CallbackBase = "https://api.openshock.app"
        var callbackUri = new Uri(new Uri(DiscordApiBase), "/1/account/oauth/callback/discord");

        // TODO: DONTIMPLEMENTYET Optional post-login returnUrl
        /*
        string? safeReturnUrl = null;
        if (returnTo is not null && IsAllowedReturnUrl(returnTo, discordOptions.AllowedReturnHosts))
        {
            safeReturnUrl = returnTo.ToString();
        }
        */

        // CSRF state (random nonce)
        var cookieContents = $"{CryptoUtils.RandomString(64)}|{returnTo}";
        var stateKeyHash = HashingUtils.HashSha256(cookieContents);

        // Persist cookies (HttpOnly, Secure, SameSite=Lax works for top-level redirects)
        SetTempCookie(StateCookieName, cookieContents);

        // Build Discord authorization URL
        var authUrl = new UriBuilder("https://discord.com/oauth2/authorize")
        {
            Query = new QueryBuilder
            {
                { "response_type", "code" },
                { "client_id",  options.ClientId },
                { "scope",       "identify" },
                { "redirect_uri", callbackUri.ToString() },
                { "state",        stateKeyHash },
            }.ToString()
        }.Uri.ToString();

        return Redirect(authUrl);
    }

    private void SetTempCookie(string name, string value)
    {
        Response.Cookies.Append(name, value, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.AddMinutes(10), // 10 minutes is plenty for a round-trip
            Path = "/"
        });
    }
}