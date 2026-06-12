using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Requests;
using OpenShock.API.Models.Response;
using OpenShock.API.Services.Token;
using OpenShock.Common.Errors;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.Problems;
using OpenShock.Common.Services.Audit;
using OpenShock.Common.Utils;

namespace OpenShock.API.Controller.Tokens;

public sealed partial class TokensController
{
    /// <summary>
    /// List all tokens for the current user
    /// </summary>
    /// <response code="200">All tokens for the current user</response>
    [HttpGet]
    [MapToApiVersion("1")]
    public IAsyncEnumerable<TokenResponse> ListTokens([FromServices] IApiTokenService tokenService)
    {
        return tokenService.ListTokensV1(ownerId: CurrentUser.Id);
    }

    /// <summary>
    /// List all tokens for the current user
    /// </summary>
    /// <response code="200">All tokens for the current user</response>
    [HttpGet]
    [MapToApiVersion("2")]
    public IAsyncEnumerable<TokenResponseV2> ListTokensV2([FromServices] IApiTokenService tokenService)
    {
        return tokenService.ListTokensV2(ownerId: CurrentUser.Id);
    }

    /// <summary>
    /// Get a token by id
    /// </summary>
    /// <param name="tokenId"></param>
    /// <param name="tokenService"></param>
    /// <response code="200">The token</response>
    /// <response code="404">The token does not exist or you do not have access to it.</response>
    [HttpGet("{tokenId}")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ApiTokenNotFound
    [MapToApiVersion("1")]
    public async Task<IActionResult> GetTokenById([FromRoute] Guid tokenId, [FromServices] IApiTokenService tokenService)
    {
        var apiToken = await tokenService.GetTokenV1(tokenId, CurrentUser.Id);
        if (apiToken is null) return Problem(ApiTokenError.ApiTokenNotFound);

        return Ok(apiToken);
    }

    /// <summary>
    /// Get a token by id
    /// </summary>
    /// <param name="tokenId"></param>
    /// <param name="tokenService"></param>
    /// <response code="200">The token</response>
    /// <response code="404">The token does not exist or you do not have access to it.</response>
    [HttpGet("{tokenId}")]
    [ProducesResponseType<TokenResponseV2>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ApiTokenNotFound
    [MapToApiVersion("2")]
    public async Task<IActionResult> GetTokenByIdV2([FromRoute] Guid tokenId, [FromServices] IApiTokenService tokenService)
    {
        var apiToken = await tokenService.GetTokenV2(tokenId, CurrentUser.Id);
        if (apiToken is null) return Problem(ApiTokenError.ApiTokenNotFound);

        return Ok(apiToken);
    }

    /// <summary>
    /// Create a new token
    /// </summary>
    /// <param name="body"></param>
    /// <param name="tokenService"></param>
    /// <response code="200">The created token</response>
    [HttpPost]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [MapToApiVersion("1")]
    public async Task<TokenCreatedResponse> CreateToken(
        [FromBody] CreateTokenRequest body,
        [FromServices] IApiTokenService tokenService,
        [FromServices] IAuditService auditService)
    {
        var result = await tokenService.CreateTokenV1(CurrentUser.Id, HttpContext.GetRemoteIP(), body);
        await auditService.LogAsync(
            CurrentUser.Id,
            AuditAction.ApiTokenCreated,
            ipAddress: HttpContext.GetRemoteIP(),
            userAgent: HttpContext.GetUserAgent(),
            metadata: new ApiTokenCreatedMetadata(result.Id, result.Name, result.Permissions.Select(p => p.ToString()).ToList()));
        return result;
    }

    /// <summary>
    /// Edit a token
    /// </summary>
    /// <param name="tokenId"></param>
    /// <param name="body"></param>
    /// <param name="tokenService"></param>
    /// <response code="200">The edited token</response>
    /// <response code="404">The token does not exist or you do not have access to it.</response>
    [HttpPatch("{tokenId}")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ApiTokenNotFound
    [MapToApiVersion("1")]
    public async Task<IActionResult> EditToken([FromRoute] Guid tokenId, [FromBody] EditTokenRequest body, [FromServices] IApiTokenService tokenService)
    {
        if (!await tokenService.EditTokenV1(tokenId, body, CurrentUser.Id)) return Problem(ApiTokenError.ApiTokenNotFound);

        return Ok();
    }

    /// <summary>
    /// Create a new token
    /// </summary>
    /// <param name="body"></param>
    /// <param name="tokenService"></param>
    /// <response code="200">The created token</response>
    [HttpPost]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [MapToApiVersion("2")]
    public async Task<TokenCreatedResponseV2> CreateTokenV2(
        [FromBody] CreateTokenRequestV2 body,
        [FromServices] IApiTokenService tokenService,
        [FromServices] IAuditService auditService)
    {
        var result = await tokenService.CreateTokenV2(CurrentUser.Id, HttpContext.GetRemoteIP(), body);
        await auditService.LogAsync(
            CurrentUser.Id,
            AuditAction.ApiTokenCreated,
            ipAddress: HttpContext.GetRemoteIP(),
            userAgent: HttpContext.GetUserAgent(),
            metadata: new ApiTokenCreatedMetadata(result.Id, result.Name, result.Permissions.Select(p => p.ToString()).ToList()));
        return result;
    }

    /// <summary>
    /// Edit a token
    /// </summary>
    /// <param name="tokenId"></param>
    /// <param name="body"></param>
    /// <param name="tokenService"></param>
    /// <response code="200">The edited token</response>
    /// <response code="404">The token does not exist or you do not have access to it.</response>
    [HttpPatch("{tokenId}")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ApiTokenNotFound
    [MapToApiVersion("2")]
    public async Task<IActionResult> EditTokenV2([FromRoute] Guid tokenId, [FromBody] EditTokenRequestV2 body, [FromServices] IApiTokenService tokenService)
    {
        if (!await tokenService.EditTokenV2(tokenId, body, CurrentUser.Id)) return Problem(ApiTokenError.ApiTokenNotFound);

        return Ok();
    }

    /// <summary>
    /// Set whether a token is paused. A paused token may not send shocker control messages.
    /// </summary>
    /// <param name="tokenId"></param>
    /// <param name="body"></param>
    /// <param name="tokenService"></param>
    /// <response code="200">The now-set paused state of the token.</response>
    /// <response code="404">The token does not exist or you do not have access to it.</response>
    [HttpPatch("{tokenId}/paused")]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType<TokenPausedResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ApiTokenNotFound
    [MapToApiVersion("2")]
    public async Task<IActionResult> SetTokenPaused([FromRoute] Guid tokenId, [FromBody] SetTokenPausedRequest body, [FromServices] IApiTokenService tokenService)
    {
        var paused = await tokenService.SetTokenPaused(tokenId, body.Paused, CurrentUser.Id);
        if (paused is null) return Problem(ApiTokenError.ApiTokenNotFound);

        return Ok(new TokenPausedResponse { Paused = paused.Value });
    }
}
