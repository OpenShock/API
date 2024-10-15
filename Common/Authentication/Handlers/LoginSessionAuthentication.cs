using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.Errors;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;
using OpenShock.Common.Redis;
using OpenShock.Common.Services.BatchUpdate;
using OpenShock.Common.Utils;
using Redis.OM.Contracts;
using Redis.OM.Searching;

namespace OpenShock.Common.Authentication.Handlers;

public sealed class LoginSessionAuthentication : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IClientAuthService<LinkUser> _authService;
    private readonly ITokenReferenceService<ApiToken> _tokenReferenceService;
    private readonly IBatchUpdateService _batchUpdateService;
    private readonly OpenShockContext _db;
    private readonly IRedisCollection<LoginSession> _userSessions;
    private readonly JsonSerializerOptions _serializerOptions;
    private OpenShockProblem? _authResultError = null;

    public LoginSessionAuthentication(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IClientAuthService<LinkUser> clientAuth,
        OpenShockContext db,
        IRedisConnectionProvider provider,
        IOptions<JsonOptions> jsonOptions, ITokenReferenceService<ApiToken> tokenReferenceService, IBatchUpdateService batchUpdateService)
        : base(options, logger, encoder)
    {
        _authService = clientAuth;
        _db = db;
        _tokenReferenceService = tokenReferenceService;
        _batchUpdateService = batchUpdateService;
        _userSessions = provider.RedisCollection<LoginSession>();
        _serializerOptions = jsonOptions.Value.SerializerOptions;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if ((Context.Request.Headers.TryGetValue("OpenShockToken", out var tokenHeaderO) || Context.Request.Headers.TryGetValue("Open-Shock-Token", out tokenHeaderO)) &&
            !string.IsNullOrEmpty(tokenHeaderO)) return TokenAuth(tokenHeaderO!);
        
        if (Context.Request.Headers.TryGetValue("OpenShockSession", out var sessionKeyHeader) &&
            !string.IsNullOrEmpty(sessionKeyHeader)) return SessionAuth(sessionKeyHeader!);
        
        if (Context.Request.Cookies.TryGetValue("openShockSession", out var accessKeyCookie) &&
            !string.IsNullOrEmpty(accessKeyCookie)) return SessionAuth(accessKeyCookie);
        
        // Legacy to not break current applications
        if (Context.Request.Headers.TryGetValue("ShockLinkToken", out var tokenHeader) &&
            !string.IsNullOrEmpty(tokenHeader)) return TokenAuth(tokenHeader!);

        return Task.FromResult(Fail(AuthResultError.HeaderMissingOrInvalid));
    }

    private async Task<AuthenticateResult> TokenAuth(string token)
    {
        var tokenDto = await _db.ApiTokens.Include(x => x.User).SingleOrDefaultAsync(x => x.Token == token &&
            (x.ValidUntil == null || x.ValidUntil >= DateTime.UtcNow));
        if (tokenDto == null) return Fail(AuthResultError.TokenInvalid);

        _batchUpdateService.UpdateTokenLastUsed(tokenDto.Id);
        _authService.CurrentClient = new LinkUser
        {
            DbUser = tokenDto.User
        };
        _tokenReferenceService.Token = tokenDto;

        Context.Items["User"] = _authService.CurrentClient.DbUser.Id;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _authService.CurrentClient.DbUser.Id.ToString()),
            new(ControlLogAdditionalItem.ApiTokenId, tokenDto.Id.ToString())
        };

        var ident = new ClaimsIdentity(claims, nameof(LoginSessionAuthentication));
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(ident), Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    private async Task<AuthenticateResult> SessionAuth(string sessionKey)
    {
        var session = await _userSessions.FindByIdAsync(sessionKey);
        if (session == null) return Fail(AuthResultError.SessionInvalid);

        // This can be removed at a later point, this is just for upgrade purposes
        if(UpdateOlderLoginSessions(session)) await _userSessions.SaveAsync();

        if (session.Expires!.Value < DateTime.UtcNow.Subtract(Constants.LoginSessionExpansionAfter))
        {
#pragma warning disable CS4014
            LucTask.Run(async () =>
#pragma warning restore CS4014
            {
                session.Expires = DateTime.UtcNow.Add(Constants.LoginSessionLifetime);
                await _userSessions.UpdateAsync(session, Constants.LoginSessionLifetime);
            });
        }
        
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

        var ident = new ClaimsIdentity(claims, nameof(LoginSessionAuthentication));
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(ident), Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
    

    private AuthenticateResult Fail(OpenShockProblem reason)
    {
        _authResultError = reason;
        return AuthenticateResult.Fail(reason.Type!);
    }

    /// <inheritdoc />
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        _authResultError ??= AuthResultError.UnknownError;
        Response.StatusCode = _authResultError.Status!.Value;
        _authResultError.AddContext(Context);
        return Context.Response.WriteAsJsonAsync(_authResultError, _serializerOptions, contentType: "application/problem+json");
    }
    
    public static bool UpdateOlderLoginSessions(LoginSession session)
    {
        var save = false;
        
        if (session.PublicId == null)
        {
            session.PublicId = Guid.NewGuid();
            save = true;
        }

        if (session.Created == null)
        {
            session.Created = DateTime.UtcNow;
            save = true;
        }

        if (session.Expires == null)
        {
            session.Expires = DateTime.UtcNow.Add(Constants.LoginSessionLifetime);
            save = true;
        }

        return save;
    }
}