using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Response;
using OpenShock.API.Utils;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.ServicesCommon.Authentication;

namespace OpenShock.API.Controller.Tokens;

public sealed partial class TokensController
{
    /// <summary>
    /// List all tokens for the current user
    /// </summary>
    /// <response code="200">All tokens for the current user</response>
    [HttpGet]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<BaseResponse<IEnumerable<TokenResponse>>> ListTokens()
    {
        var apiTokens = await _db.ApiTokens.Where(x => x.UserId == CurrentUser.DbUser.Id).OrderBy(x => x.CreatedOn).Select(x => new TokenResponse
        {
            CreatedByIp = x.CreatedByIp,
            CreatedOn = x.CreatedOn,
            ValidUntil = x.ValidUntil,
            Permissions = x.Permissions,
            Name = x.Name,
            Id = x.Id
        }).ToListAsync();

        return new BaseResponse<IEnumerable<TokenResponse>>
        {
            Data = apiTokens
        };
    }

    /// <summary>
    /// Get a token by id
    /// </summary>
    /// <param name="tokenId"></param>
    /// <response code="200">The token</response>
    /// <response code="404">The token does not exist or you do not have access to it.</response>
    [HttpGet("{tokenId}")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<BaseResponse<TokenResponse>> GetTokenById([FromRoute] Guid tokenId)
    {
        var apiToken = await _db.ApiTokens.Where(x => x.UserId == CurrentUser.DbUser.Id && x.Id == tokenId).Select(x => new TokenResponse
        {
            CreatedByIp = x.CreatedByIp,
            CreatedOn = x.CreatedOn,
            ValidUntil = x.ValidUntil,
            Permissions = x.Permissions,
            Name = x.Name,
            Id = x.Id
        }).FirstOrDefaultAsync();
        if (apiToken == null)
            return EBaseResponse<TokenResponse>("Api Token could not be found", HttpStatusCode.NotFound);
        return new BaseResponse<TokenResponse>
        {
            Data = apiToken
        };
    }

    /// <summary>
    /// Revoke a token from the current user
    /// </summary>
    /// <param name="tokenId"></param>
    /// <response code="200">Successfully deleted token</response>
    /// <response code="404">The token does not exist or you do not have access to it.</response>
    [HttpDelete("{tokenId}")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<BaseResponse<TokenResponse>> DeleteToken([FromRoute] Guid tokenId)
    {
        var apiToken = await _db.ApiTokens.Where(x => x.UserId == CurrentUser.DbUser.Id && x.Id == tokenId)
            .ExecuteDeleteAsync();
        if (apiToken <= 0) return EBaseResponse<TokenResponse>("Api Token could not be found", HttpStatusCode.NotFound);
        return new BaseResponse<TokenResponse>
        {
            Message = "Successfully deleted api token"
        };
    }

    /// <summary>
    /// Create a new token
    /// </summary>
    /// <param name="body"></param>
    /// <response code="200">The created token</response>
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<BaseResponse<string>> CreateToken([FromBody] CreateTokenRequest body)
    {
        var token = new ApiToken
        {
            UserId = CurrentUser.DbUser.Id,
            Token = CryptoUtils.RandomString(64),
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "error",
            Permissions = body.Permissions,
            Id = Guid.NewGuid(),
            Name = body.Name,
            ValidUntil = body.ValidUntil
        };
        _db.ApiTokens.Add(token);
        await _db.SaveChangesAsync();

        return new BaseResponse<string>
        {
            Data = token.Token
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
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<BaseResponse<object>> EditToken([FromRoute] Guid tokenId, [FromBody] EditTokenRequest body)
    {
        var token = await _db.ApiTokens.FirstOrDefaultAsync(x => x.UserId == CurrentUser.DbUser.Id && x.Id == tokenId);
        if (token == null) return EBaseResponse<object>("API token does not exist", HttpStatusCode.NotFound);

        token.Name = body.Name;
        token.Permissions = body.Permissions;
        await _db.SaveChangesAsync();

        return new BaseResponse<object>("Successfully updated api token");
    }

    public class EditTokenRequest
    {
        public required string Name { get; set; }
        public List<PermissionType> Permissions { get; set; } = PermissionTypeBindings.AllPermissionTypes;
    }

    public class CreateTokenRequest
    {
        public required string Name { get; set; }
        public List<PermissionType> Permissions { get; set; } = PermissionTypeBindings.AllPermissionTypes;
        public DateOnly? ValidUntil { get; set; }
    }
}