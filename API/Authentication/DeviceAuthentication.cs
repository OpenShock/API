using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using ShockLink.API.Models;
using ShockLink.Common.Redis;
using ShockLink.Common.ShockLinkDb;
using Device = ShockLink.API.Models.Response.Device;

namespace ShockLink.API.Authentication;

public class DeviceAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
}

public class DeviceAuthentication : AuthenticationHandler<DeviceAuthenticationSchemeOptions>
{
    private readonly IClientAuthService<ShockLink.Common.ShockLinkDb.Device> _authService;
    private readonly ShockLinkContext _db;
    private string _failReason = "Internal server error";

    public DeviceAuthentication(IOptionsMonitor<DeviceAuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, IClientAuthService<ShockLink.Common.ShockLinkDb.Device> clientAuth, ShockLinkContext db)
        : base(options, logger, encoder, clock)
    {
        _authService = clientAuth;
        _db = db;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string sessionKey;

        if (Context.Request.Headers.TryGetValue("DeviceToken", out var sessionKeyHeader) &&
            !string.IsNullOrEmpty(sessionKeyHeader))
        {
            sessionKey = sessionKeyHeader!;
        }
        else return Fail("DeviceToken header was not found");

        var device = await _db.Devices.Where(x => x.Token == sessionKey).SingleOrDefaultAsync();
        if (device == null) return Fail("No device associated with device token");

        _authService.CurrentClient = device;
        Context.Items["Device"] = _authService.CurrentClient.Id;

        var claims = new[]
        {
            new Claim("id", _authService.CurrentClient.Id.ToString()),
        };
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