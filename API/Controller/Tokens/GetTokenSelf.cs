using System.Diagnostics;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models;
using OpenShock.API.Models.Response;
using OpenShock.Common.Authentication;
using OpenShock.Common.Authentication.ControllerBase;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Tokens;

[ApiController]
[Tags("API Tokens")]
[Route("/{version:apiVersion}/tokens")]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemes.ApiToken)]
[ApiVersion("1"), ApiVersion("2")]
public sealed partial class TokensSelfController : AuthenticatedSessionControllerBase
{
    /// <summary>
    /// Gets information about the current token used to access this endpoint
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    [HttpGet("self")]
    [MapToApiVersion("1")]
    public TokenResponse GetSelfToken()
    {
        return TokenResponse.MapFrom(GetSelfTokenV2());
    }

    /// <summary>
    /// Gets information about the current token used to access this endpoint
    /// </summary>
    /// <returns></returns>
    [HttpGet("self")]
    [MapToApiVersion("2")]
    public TokenResponseV2 GetSelfTokenV2()
    {
        var token = GetRequiredItem<ApiToken>();

        return new TokenResponseV2
        {
            CreatedOn = token.CreatedAt,
            ValidUntil = token.ValidUntil,
            LastUsed = token.LastUsed,
            Permissions = token.Permissions,
            Name = token.Name,
            Id = token.Id,
            ShockerControl = ShockerControlSettings.FromToken(token)
        };
    }

}