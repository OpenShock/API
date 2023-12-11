using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Response;
using OpenShock.API.Utils;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.ServicesCommon.Authentication;

namespace OpenShock.API.Controller.Tokens;

partial class TokensController
{
    /// <summary>
    /// Gets all tokens for the current user
    /// </summary>
    /// <response code="200">All tokens for the current user</response>
    [HttpGet(Name = "GetTokens")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<BaseResponse<IEnumerable<ApiTokenResponse>>> GetTokens()
    {
        var apiTokens = await _db.ApiTokens.Where(x => x.UserId == CurrentUser.DbUser.Id).OrderBy(x => x.CreatedOn).Select(x => new ApiTokenResponse
        {
            CreatedByIp = x.CreatedByIp,
            CreatedOn = x.CreatedOn,
            ValidUntil = x.ValidUntil,
            Permissions = x.Permissions,
            Name = x.Name,
            Id = x.Id
        }).ToListAsync();

        return new BaseResponse<IEnumerable<ApiTokenResponse>>
        {
            Data = apiTokens
        };
    }

    /// <summary>
    /// Gets a token by id
    /// </summary>
    /// <param name="id"></param>
    /// <response code="200">The token</response>
    /// <response code="404">The token does not exist or you do not have access to it.</response>
    [HttpGet("{id}", Name = "GetToken")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<BaseResponse<ApiTokenResponse>> GetToken([FromRoute] Guid id)
    {
        var apiToken = await _db.ApiTokens.Where(x => x.UserId == CurrentUser.DbUser.Id && x.Id == id).Select(x => new ApiTokenResponse
        {
            CreatedByIp = x.CreatedByIp,
            CreatedOn = x.CreatedOn,
            ValidUntil = x.ValidUntil,
            Permissions = x.Permissions,
            Name = x.Name,
            Id = x.Id
        }).FirstOrDefaultAsync();
        if (apiToken == null)
            return EBaseResponse<ApiTokenResponse>("Api Token could not be found", HttpStatusCode.NotFound);
        return new BaseResponse<ApiTokenResponse>
        {
            Data = apiToken
        };
    }

    /// <summary>
    /// Deletes a token
    /// </summary>
    /// <param name="id"></param>
    /// <response code="200">Successfully deleted token</response>
    /// <response code="404">The token does not exist or you do not have access to it.</response>
    [HttpDelete("{id}", Name = "DeleteToken")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<BaseResponse<ApiTokenResponse>> DeleteToken([FromRoute] Guid id)
    {
        var apiToken = await _db.ApiTokens.Where(x => x.UserId == CurrentUser.DbUser.Id && x.Id == id)
            .ExecuteDeleteAsync();
        if (apiToken <= 0) return EBaseResponse<ApiTokenResponse>("Api Token could not be found", HttpStatusCode.NotFound);
        return new BaseResponse<ApiTokenResponse>
        {
            Message = "Successfully deleted api token"
        };
    }

    /// <summary>
    /// Creates a new token
    /// </summary>
    /// <param name="data"></param>
    /// <response code="200">The created token</response>
    [HttpPost(Name = "CreateToken")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<BaseResponse<string>> CreateToken([FromBody] CreateTokenRequest data)
    {
        var token = new ApiToken
        {
            UserId = CurrentUser.DbUser.Id,
            Token = CryptoUtils.RandomString(64),
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "error",
            Permissions = data.Permissions,
            Id = Guid.NewGuid(),
            Name = data.Name,
            ValidUntil = data.ValidUntil
        };
        _db.ApiTokens.Add(token);
        await _db.SaveChangesAsync();

        return new BaseResponse<string>
        {
            Data = token.Token
        };
    }

    /// <summary>
    /// Edits a token
    /// </summary>
    /// <param name="id"></param>
    /// <param name="data"></param>
    /// <response code="200">The edited token</response>
    /// <response code="404">The token does not exist or you do not have access to it.</response>
    [HttpPatch("{id}", Name = "EditToken")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<BaseResponse<object>> EditToken([FromRoute] Guid id, [FromBody] EditTokenRequest data)
    {
        var token = await _db.ApiTokens.FirstOrDefaultAsync(x => x.UserId == CurrentUser.DbUser.Id && x.Id == id);
        if (token == null) return EBaseResponse<object>("API token does not exist", HttpStatusCode.NotFound);

        token.Name = data.Name;
        token.Permissions = data.Permissions;
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