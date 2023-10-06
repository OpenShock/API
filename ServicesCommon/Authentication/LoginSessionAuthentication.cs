using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using Redis.OM.Contracts;
using Redis.OM.Searching;

namespace OpenShock.ServicesCommon.Authentication;

public class LoginSessionAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
}

public class LoginSessionAuthentication : AuthenticationHandler<LoginSessionAuthenticationSchemeOptions>
{
    private readonly IClientAuthService<LinkUser> _authService;
    private readonly IMemoryCache _memoryCache;
    private readonly OpenShockContext _db;
    private readonly IRedisCollection<LoginSession> _userSessions;
    private string _failReason = "Internal server error";

    public LoginSessionAuthentication(IOptionsMonitor<LoginSessionAuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, IClientAuthService<LinkUser> clientAuth,
        IMemoryCache memoryCache, OpenShockContext db, IRedisConnectionProvider provider)
        : base(options, logger, encoder, clock)
    {
        _authService = clientAuth;
        _memoryCache = memoryCache;
        _db = db;
        _userSessions = provider.RedisCollection<LoginSession>(false);
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Context.Request.Query.TryGetValue("session", out var getSession) &&
            !string.IsNullOrEmpty(getSession)) return SessionAuth(getSession!);

        if (Context.Request.Headers.TryGetValue("OpenShockSession", out var sessionKeyHeader) &&
            !string.IsNullOrEmpty(sessionKeyHeader)) return SessionAuth(sessionKeyHeader!);

        if (Context.Request.Headers.TryGetValue("ShockLinkToken", out var tokenHeader) &&
            !string.IsNullOrEmpty(tokenHeader)) return TokenAuth(tokenHeader!);

        if (Context.Request.Headers.TryGetValue("OpenShockToken", out var tokenHeaderO) &&
            !string.IsNullOrEmpty(tokenHeaderO)) return TokenAuth(tokenHeaderO!);

        return Task.FromResult(Fail("OpenShockSession/OpenShockToken header/getparam was not found"));
    }

    private async Task<AuthenticateResult> TokenAuth(string token)
    {
        var tokenDto = await _db.ApiTokens.Include(x => x.User).SingleOrDefaultAsync(x => x.Token == token &&
            (x.ValidUntil == null || x.ValidUntil >= DateOnly.FromDateTime(DateTime.UtcNow)));
        if (tokenDto == null) return Fail("Token is not valid");

        _authService.CurrentClient = new LinkUser
        {
            DbUser = tokenDto.User
        };

        Context.Items["User"] = _authService.CurrentClient.DbUser.Id;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _authService.CurrentClient.DbUser.Id.ToString()),
            new(ControlLogAdditionalItem.ApiTokenId, tokenDto.Id.ToString())
        };
        claims.AddRange(tokenDto.Permissions.Select(tokenDtoPermission =>
            PermissionTypeBindings.TypeToName[tokenDtoPermission]));

        var ident = new ClaimsIdentity(claims, nameof(LoginSessionAuthentication));
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(ident), Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    private async Task<AuthenticateResult> SessionAuth(string sessionKey)
    {
        var session = await _userSessions.FindByIdAsync(sessionKey);
        if (session == null) return Fail("Session was not found");

        var retrievedUser = await _db.Users.FirstAsync(user => user.Id == session.UserId);

        _authService.CurrentClient = new LinkUser
        {
            DbUser = retrievedUser
        };

        Context.Items["User"] = _authService.CurrentClient.DbUser.Id;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _authService.CurrentClient.DbUser.Id.ToString())
        };
        claims.AddRange(PermissionTypeBindings.RoleClaimNames);

        var ident = new ClaimsIdentity(claims, nameof(LoginSessionAuthentication));
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(ident), Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    private AuthenticateResult Fail(string reason)
    {
        _failReason = reason;
        return AuthenticateResult.Fail(reason);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        return Context.Response.WriteAsJsonAsync(new BaseResponse<object>
        {
            Message = _failReason
        });
    }
}