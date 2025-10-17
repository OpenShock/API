using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenShock.Common.Errors;
using OpenShock.Common.Extensions;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;

namespace OpenShock.Common.Authentication.AuthenticationHandlers;

/// <summary>
/// Device / Box / The Thing / ESP32 authentication with DeviceToken header
/// </summary>
public sealed class HubAuthentication : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly OpenShockContext _db;
    
    private OpenShockProblem? _authResultError = null;


    public HubAuthentication(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        OpenShockContext db
        )
        : base(options, logger, encoder)
    {
        _db = db;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Context.TryGetHubTokenFromHeader(out var hubToken))
        {
            return Fail(AuthResultError.HeaderMissingOrInvalid);
        }

        var hub = await _db.Devices
            .Where(x => x.Token == hubToken)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.OwnerId,
                IsDeactivated = x.Owner.UserDeactivation != null
            })
            .FirstOrDefaultAsync();
        if (hub is null)
        {
            return Fail(AuthResultError.TokenInvalid);
        }
        if (hub.IsDeactivated)
        {
            return Fail(AuthResultError.AccountDeactivated);
        }

        Claim[] claims = [
            new(ClaimTypes.AuthenticationMethod, OpenShockAuthSchemes.HubToken),
            new(ClaimTypes.NameIdentifier, hub.OwnerId.ToString()),
            new(OpenShockAuthClaims.HubId, hub.Id.ToString()),
            new(OpenShockAuthClaims.HubName, hub.Name),
        ];
        
        var ident = new ClaimsIdentity(claims, OpenShockAuthSchemes.HubToken);
        var principal = new ClaimsPrincipal(ident);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

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
        return _authResultError.WriteAsJsonAsync(Context);
    }
}