using System.Linq.Expressions;
using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Requests;
using OpenShock.API.Models.Response;
using OpenShock.Common.Constants;
using OpenShock.Common.Errors;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Problems;
using OpenShock.Common.Utils;

namespace OpenShock.API.Controller.Tokens;

public sealed partial class TokensController
{
    private static readonly Expression<Func<ApiToken, TokenResponse>> ToTokenResponse = x => new TokenResponse
    {
        CreatedOn = x.CreatedAt,
        ValidUntil = x.ValidUntil,
        LastUsed = x.LastUsed ?? default,
        Permissions = x.Permissions,
        Name = x.Name,
        Id = x.Id
    };

    private static readonly Expression<Func<ApiToken, TokenResponseV2>> ToTokenResponseV2 = x => new TokenResponseV2
    {
        CreatedOn = x.CreatedAt,
        ValidUntil = x.ValidUntil,
        LastUsed = x.LastUsed,
        Permissions = x.Permissions,
        Name = x.Name,
        Id = x.Id
    };

    /// <summary>
    /// Tokens belonging to the current user that have not expired.
    /// </summary>
    private IQueryable<ApiToken> CurrentUserValidTokens => _db.ApiTokens
        .Where(x => x.UserId == CurrentUser.Id && (x.ValidUntil == null || x.ValidUntil > DateTime.UtcNow));

    /// <summary>
    /// List all tokens for the current user
    /// </summary>
    /// <response code="200">All tokens for the current user</response>
    [HttpGet]
    [MapToApiVersion("1")]
    public IAsyncEnumerable<TokenResponse> ListTokens()
    {
        return CurrentUserValidTokens
            .OrderBy(x => x.CreatedAt)
            .Select(ToTokenResponse)
            .AsAsyncEnumerable();
    }

    /// <summary>
    /// List all tokens for the current user
    /// </summary>
    /// <response code="200">All tokens for the current user</response>
    [HttpGet]
    [MapToApiVersion("2")]
    public IAsyncEnumerable<TokenResponseV2> ListTokensV2()
    {
        return CurrentUserValidTokens
            .OrderBy(x => x.CreatedAt)
            .Select(ToTokenResponseV2)
            .AsAsyncEnumerable();
    }

    /// <summary>
    /// Get a token by id
    /// </summary>
    /// <param name="tokenId"></param>
    /// <response code="200">The token</response>
    /// <response code="404">The token does not exist or you do not have access to it.</response>
    [HttpGet("{tokenId}")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ApiTokenNotFound
    [MapToApiVersion("1")]
    public async Task<IActionResult> GetTokenById([FromRoute] Guid tokenId)
    {
        var apiToken = await CurrentUserValidTokens
            .Where(x => x.Id == tokenId)
            .Select(ToTokenResponse)
            .FirstOrDefaultAsync();

        if (apiToken is null) return Problem(ApiTokenError.ApiTokenNotFound);

        return Ok(apiToken);
    }

    /// <summary>
    /// Get a token by id
    /// </summary>
    /// <param name="tokenId"></param>
    /// <response code="200">The token</response>
    /// <response code="404">The token does not exist or you do not have access to it.</response>
    [HttpGet("{tokenId}")]
    [ProducesResponseType<TokenResponseV2>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ApiTokenNotFound
    [MapToApiVersion("2")]
    public async Task<IActionResult> GetTokenByIdV2([FromRoute] Guid tokenId)
    {
        var apiToken = await CurrentUserValidTokens
            .Where(x => x.Id == tokenId)
            .Select(ToTokenResponseV2)
            .FirstOrDefaultAsync();

        if (apiToken is null) return Problem(ApiTokenError.ApiTokenNotFound);

        return Ok(apiToken);
    }

    /// <summary>
    /// Create a new token
    /// </summary>
    /// <param name="body"></param>
    /// <response code="200">The created token</response>
    [HttpPost]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [MapToApiVersion("1")]
    public async Task<TokenCreatedResponse> CreateToken([FromBody] CreateTokenRequest body)
    {
        var token = CryptoUtils.RandomAlphaNumericString(AuthConstants.ApiTokenLength);

        var tokenDto = new ApiToken
        {
            Id = Guid.CreateVersion7(),
            UserId = CurrentUser.Id,
            Name = body.Name,
            TokenHash = HashingUtils.HashToken(token),
            CreatedByIp = HttpContext.GetRemoteIP(),
            Permissions = body.Permissions.Distinct().ToList(),
            ValidUntil = body.ValidUntil?.ToUniversalTime()
        };
        _db.ApiTokens.Add(tokenDto);
        await _db.SaveChangesAsync();

        return new TokenCreatedResponse
        {
            Id = tokenDto.Id,
            Name = body.Name,
            Token = token,
            CreatedAt = tokenDto.CreatedAt,
            ValidUntil = tokenDto.ValidUntil,
            LastUsed = tokenDto.LastUsed ?? default,
            Permissions = tokenDto.Permissions
        };
    }

    /// <summary>
    /// Edit a token
    /// </summary>
    /// <param name="tokenId"></param>
    /// <param name="body"></param>
    /// <response code="200">The edited token</response>
    /// <response code="404">The token does not exist or you do not have access to it.</response>
    [HttpPatch("{tokenId}")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // ApiTokenNotFound    
    [MapToApiVersion("1")]
    public async Task<IActionResult> EditToken([FromRoute] Guid tokenId, [FromBody] EditTokenRequest body)
    {
        var token = await CurrentUserValidTokens.FirstOrDefaultAsync(x => x.Id == tokenId);
        if (token is null) return Problem(ApiTokenError.ApiTokenNotFound);

        token.Name = body.Name;
        token.Permissions = body.Permissions.Distinct().ToList();
        await _db.SaveChangesAsync();

        return Ok();
    }
}