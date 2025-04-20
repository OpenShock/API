using System.Net.Mime;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.Constants;
using OpenShock.Common.Errors;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;
using OpenShock.Common.Services.BatchUpdate;
using OpenShock.Common.Services.Session;
using OpenShock.Common.Utils;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace OpenShock.Common.Authentication.AuthenticationHandlers;

public sealed class UserSessionAuthentication : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IClientAuthService<User> _authService;
    private readonly IUserReferenceService _userReferenceService;
    private readonly IBatchUpdateService _batchUpdateService;
    private readonly OpenShockContext _db;
    private readonly ISessionService _sessionService;
    private readonly JsonSerializerOptions _serializerOptions;
    private OpenShockProblem? _authResultError = null;

    public UserSessionAuthentication(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IClientAuthService<User> clientAuth,
        IUserReferenceService userReferenceService,
        OpenShockContext db,
        ISessionService sessionService,
        IOptions<JsonOptions> jsonOptions, IBatchUpdateService batchUpdateService)
        : base(options, logger, encoder)
    {
        _authService = clientAuth;
        _userReferenceService = userReferenceService;
        _db = db;
        _sessionService = sessionService;
        _serializerOptions = jsonOptions.Value.SerializerOptions;
        _batchUpdateService = batchUpdateService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Context.TryGetUserSession(out var sessionKey))
        {
            return Fail(AuthResultError.CookieMissingOrInvalid);
        }

        var session = await _sessionService.GetSessionById(sessionKey);
        if (session == null) return Fail(AuthResultError.SessionInvalid);

        if (session.Expires!.Value < DateTime.UtcNow.Subtract(Duration.LoginSessionExpansionAfter))
        {
#pragma warning disable CS4014
            OsTask.Run(async () =>
#pragma warning restore CS4014
            {
                session.Expires = DateTime.UtcNow.Add(Duration.LoginSessionLifetime);
                await _sessionService.UpdateSession(session, Duration.LoginSessionLifetime);
            });
        }

        _batchUpdateService.UpdateSessionLastUsed(sessionKey, DateTimeOffset.UtcNow);

        var retrievedUser = await _db.Users.FirstAsync(user => user.Id == session.UserId);

        _authService.CurrentClient = retrievedUser;
        _userReferenceService.AuthReference = session;

        List<Claim> claims = [
            new(ClaimTypes.AuthenticationMethod, OpenShockAuthSchemas.UserSessionCookie),
            new(ClaimTypes.NameIdentifier, retrievedUser.Id.ToString()),
        ];

        claims.AddRange(retrievedUser.Roles.Select(r => new Claim(ClaimTypes.Role, r.ToString())));

        var ident = new ClaimsIdentity(claims, nameof(UserSessionAuthentication));

        Context.User = new ClaimsPrincipal(ident);

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
        if (Context.Response.HasStarted) return Task.CompletedTask;
        _authResultError ??= AuthResultError.UnknownError;
        Response.StatusCode = _authResultError.Status!.Value;
        _authResultError.AddContext(Context);
        return Context.Response.WriteAsJsonAsync(_authResultError, _serializerOptions, contentType: MediaTypeNames.Application.ProblemJson);
    }
}