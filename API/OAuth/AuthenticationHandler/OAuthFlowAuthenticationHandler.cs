using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using OpenShock.API.OAuth.FlowStore;

namespace OpenShock.API.OAuth.AuthenticationHandler;

public sealed class OAuthFlowAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>, IAuthenticationSignInHandler
{
    public const string CookieName = ".OpenShock.OAuthFlow";
    
    private readonly IOAuthFlowStore _store;

    public OAuthFlowAuthenticationHandler(
        IOAuthFlowStore store,
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
        _store = store;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(CookieName, out var flowId) || string.IsNullOrWhiteSpace(flowId))
        {
            DeleteCookie();
            return AuthenticateResult.NoResult();
        }

        var snapshot = await _store.GetAsync(flowId);
        if (snapshot is null)
        {
            // stale cookie: nuke it
            await DeleteSession(flowId);
            return AuthenticateResult.NoResult();
        }

        List<Claim> claims = [
            new("flow_id", snapshot.FlowId),
            new("provider", snapshot.Provider),
            new(ClaimTypes.NameIdentifier, snapshot.ExternalId, ClaimValueTypes.String, snapshot.Provider),
        ];
        if (!string.IsNullOrEmpty(snapshot.Email)) claims.Add(new Claim(ClaimTypes.Email, snapshot.Email, ClaimValueTypes.String, snapshot.Provider));
        if (!string.IsNullOrEmpty(snapshot.DisplayName)) claims.Add(new Claim(ClaimTypes.Name, snapshot.DisplayName, ClaimValueTypes.String, snapshot.Provider));

        var ident = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(ident);

        var props = new AuthenticationProperties
        {
            IssuedUtc = snapshot.IssuedUtc,
            ExpiresUtc = snapshot.IssuedUtc.Add(OAuthConstants.StateLifetime),
            IsPersistent = false,
        };
        
        var ticket = new AuthenticationTicket(principal, props, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    public async Task SignInAsync(ClaimsPrincipal user, AuthenticationProperties? properties)
    {
        var issuedUtc = properties?.IssuedUtc ?? DateTimeOffset.UtcNow;
        
        var idn = user.Identities.Single();
        var provider = idn.Claims.FirstOrDefault()?.Issuer
            ?? idn?.AuthenticationType
            ?? "external";
        
        var externalId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("Missing external subject (NameIdentifier).");

        var email       = user.FindFirst(ClaimTypes.Email)?.Value;
        var displayName = user.Identity?.Name;

        // Persist minimal snapshot (tokens, if any, handled by your store overloads)
        var flowId = await _store.SaveAsync(provider, externalId, email, displayName, issuedUtc);

        // Hand off to browser via short-lived HttpOnly cookie
        SetCookie(flowId, issuedUtc.Add(OAuthConstants.StateLifetime));
    }

    // ===== sign-out (remove from redis + clear cookie) =====
    public async Task SignOutAsync(AuthenticationProperties? properties)
    {
        if (Request.Cookies.TryGetValue(CookieName, out var flowId) && !string.IsNullOrWhiteSpace(flowId))
        {
            await DeleteSession(flowId);
        }
        else
        {
            DeleteCookie();
        }
    }

    // not really used for this temp scheme; return harmless codes
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
    
    private void SetCookie(string flowId, DateTimeOffset expires)
    {
        Response.Cookies.Append(
            CookieName,
            flowId,
            new CookieOptions
            {
                Path = "/",
                Secure = true,
                SameSite = SameSiteMode.Lax, // TODO: This should probably be way more secure?
                HttpOnly = true,
                IsEssential = true,
                Expires = expires
            }
        );
    }

    private void DeleteCookie()
    {
        Response.Cookies.Delete(CookieName, new CookieOptions { Path = "/" });
    }
    private async Task DeleteSession(string flowId)
    {
        await _store.DeleteAsync(flowId);
        DeleteCookie();
    }
}
