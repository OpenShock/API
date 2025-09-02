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
        if (!Context.TryGetUserSessionToken(out var sessionToken))
        {
            return Fail(AuthResultError.CookieMissingOrInvalid);
        }

        var session = await _sessionService.GetSessionByTokenAsync(sessionToken);
        if (session is null) return Fail(AuthResultError.SessionInvalid);

        if (session.Expires!.Value < DateTime.UtcNow.Subtract(Duration.LoginSessionExpansionAfter))
        {
            session.Expires = DateTime.UtcNow.Add(Duration.LoginSessionLifetime);
            await _sessionService.UpdateSessionAsync(session, Duration.LoginSessionLifetime); // Yes, this means a bit more waiting, but that's for max one request a day, it's fineeeee
        }

        _batchUpdateService.UpdateSessionLastUsed(sessionToken, DateTimeOffset.UtcNow);

        var retrievedUser = await _db.Users.Include(u => u.UserDeactivation).FirstOrDefaultAsync(user => user.Id == session.UserId);
        if (retrievedUser == null) return Fail(AuthResultError.SessionInvalid);
        if (retrievedUser.ActivatedAt is null)
        {
            await _sessionService.DeleteSessionAsync(session);
            return Fail(AuthResultError.AccountNotActivated);
        }
        if (retrievedUser.UserDeactivation is not null)
        {
            await _sessionService.DeleteSessionAsync(session);
            return Fail(AuthResultError.AccountDeactivated);
        }

        _authService.CurrentClient = retrievedUser;
        _userReferenceService.AuthReference = session;

        List<Claim> claims = [
            new(ClaimTypes.AuthenticationMethod, OpenShockAuthSchemes.UserSessionCookie),
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