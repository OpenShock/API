using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Response;
using OpenShock.API.Utils;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.ServicesCommon.Authentication;

namespace OpenShock.API.Controller;

[ApiController]
[Route("/{version:apiVersion}/tokens")]
public class TokenController : AuthenticatedSessionControllerBase
{
    private readonly OpenShockContext _db;
    
    public TokenController(OpenShockContext db)
    {
        _db = db;
    }
    
    [HttpGet]
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

    [HttpGet("{id:guid}")]
    public async Task<BaseResponse<ApiTokenResponse>> GetToken(Guid id)
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

    [HttpDelete("{id:guid}")]
    public async Task<BaseResponse<ApiTokenResponse>> DeleteToken(Guid id)
    {
        var apiToken = await _db.ApiTokens.Where(x => x.UserId == CurrentUser.DbUser.Id && x.Id == id)
            .ExecuteDeleteAsync();
        if (apiToken <= 0) return EBaseResponse<ApiTokenResponse>("Api Token could not be found", HttpStatusCode.NotFound);
        return new BaseResponse<ApiTokenResponse>
        {
            Message = "Successfully deleted api token"
        };
    }

    [HttpPost]
    public async Task<BaseResponse<string>> CreateToken(CreateTokenRequest data)
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
    
    [HttpPatch("{id:guid}")]
    public async Task<BaseResponse<object>> EditToken(Guid id, EditTokenRequest data)
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