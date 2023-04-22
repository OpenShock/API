using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShockLink.API.Authentication;
using ShockLink.API.Models;
using ShockLink.API.Models.Response;
using ShockLink.API.Utils;
using ShockLink.Common.Models;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller;

[ApiController]
[Route("/{version:apiVersion}/tokens")]
public class TokenController : AuthenticatedSessionControllerBase
{
    private readonly ShockLinkContext _db;
    
    public TokenController(ShockLinkContext db)
    {
        _db = db;
    }
    
    [HttpGet]
    public async Task<BaseResponse<IEnumerable<ApiTokenResponse>>> GetTokens()
    {
        var apiTokens = await _db.ApiTokens.Where(x => x.UserId == CurrentUser.DbUser.Id).Select(x => new ApiTokenResponse
        {
            Token = x.Token,
            CreatedByIp = x.CreatedByIp,
            CreatedOn = x.CreatedOn,
            ValidUntil = x.ValidUntil,
            Permissions = x.Permissions,
            Name = x.Name,
            Id = x.Id
        }).ToListAsync();

        return new BaseResponse<IEnumerable<ApiTokenResponse>>()
        {
            Data = apiTokens
        };
    }

    [HttpGet("{id:guid}")]
    public async Task<BaseResponse<ApiTokenResponse>> GetToken()
    {
        return new BaseResponse<ApiTokenResponse>();
    }

    [HttpPost]
    public async Task<BaseResponse<ApiTokenResponse>> CreateToken(CreateTokenRequest data)
    {
        var token = new ApiToken
        {
            UserId = CurrentUser.DbUser.Id,
            Token = CryptoUtils.RandomString(64),
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "error",
            Permissions = new List<PermissionType>
            {
                PermissionType.ShockersUse
            },
            Id = Guid.NewGuid(),
            Name = data.Name,
            ValidUntil = data.ValidUntil
        };
        _db.ApiTokens.Add(token);
        await _db.SaveChangesAsync();
        
        return new BaseResponse<ApiTokenResponse>
        {
            Data = new ApiTokenResponse
            {
                Token = token.Token,
                CreatedByIp = token.CreatedByIp,
                CreatedOn = token.CreatedOn,
                ValidUntil = token.ValidUntil,
                Permissions = token.Permissions,
                Name = token.Name,
                Id = token.Id
            }
        };
    }
    
    public class CreateTokenRequest
    {
        public required string Name { get; set; }
        public DateTime? ValidUntil { get; set; }
    }
}