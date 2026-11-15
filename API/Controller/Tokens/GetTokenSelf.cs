using System.Diagnostics;
using OpenShock.Common.ApiVersioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models;
using OpenShock.API.Models.Response;
using OpenShock.Common.Authentication;
using OpenShock.Common.Authentication.ControllerBase;
using OpenShock.Common.Authentication.Services;
using OpenShock.Common.OpenShockDb;

namespace OpenShock.API.Controller.Tokens;

[ApiController]
[Tags("API Tokens")]
[Route("/{version:apiVersion}/tokens")]
[Authorize(AuthenticationSchemes = OpenShockAuthSchemes.ApiToken)]
public sealed partial class TokensSelfController : AuthenticatedSessionControllerBase
{
    /// <summary>
    /// Gets information about the current token used to access this endpoint
    /// </summary>
    /// <param name="userReferenceService"></param>
    /// <exception cref="Exception"></exception>
    [HttpGet("self")]
    [ApiVersion(1)]
    public TokenResponse GetSelfToken([FromServices] IUserReferenceService userReferenceService)
    {
        return TokenResponse.MapFrom(GetSelfTokenV2(userReferenceService));
    }

    /// <summary>
    /// Gets information about the current token used to access this endpoint
    /// </summary>
    /// <param name="userReferenceService"></param>
    /// <exception cref="Exception"></exception>
    [HttpGet("self")]
    [ApiVersion(2)]
    public TokenResponseV2 GetSelfTokenV2([FromServices] IUserReferenceService userReferenceService)
    {
        var token = GetSelfTokenDto(userReferenceService);

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

    private static ApiToken GetSelfTokenDto(IUserReferenceService userReferenceService)
    {
        if (userReferenceService.AuthReference is not ApiToken apiToken)
            throw new UnreachableException("the [TokenOnly] attribute should have blocked caller");

        return apiToken;
    }
}