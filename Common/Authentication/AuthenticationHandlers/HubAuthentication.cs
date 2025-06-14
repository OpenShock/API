﻿using System.Net.Mime;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.Errors;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;
using OpenShock.Common.Utils;

namespace OpenShock.Common.Authentication.AuthenticationHandlers;

/// <summary>
/// Device / Box / The Thing / ESP32 authentication with DeviceToken header
/// </summary>
public sealed class HubAuthentication : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IClientAuthService<Device> _authService;
    private readonly OpenShockContext _db;
    
    private readonly JsonSerializerOptions _serializerOptions;
    private OpenShockProblem? _authResultError = null;


    public HubAuthentication(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IClientAuthService<Device> clientAuth,
        OpenShockContext db,
        IOptions<JsonOptions> jsonOptions)
        : base(options, logger, encoder)
    {
        _authService = clientAuth;
        _db = db;
        _serializerOptions = jsonOptions.Value.SerializerOptions;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Context.TryGetDeviceTokenFromHeader(out var sessionKey))
        {
            return Fail(AuthResultError.HeaderMissingOrInvalid);
        }

        var device = await _db.Devices.FirstOrDefaultAsync(x => x.Token == sessionKey);
        if (device is null)
        {
            return Fail(AuthResultError.TokenInvalid);
        }

        _authService.CurrentClient = device;

        Claim[] claims = [
            new(ClaimTypes.AuthenticationMethod, OpenShockAuthSchemas.HubToken),
            new Claim(ClaimTypes.NameIdentifier, device.OwnerId.ToString()),
            new Claim(OpenShockAuthClaims.HubId, _authService.CurrentClient.Id.ToString()),
        ];
        var ident = new ClaimsIdentity(claims, nameof(HubAuthentication));
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