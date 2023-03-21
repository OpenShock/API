using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using ShockLink.API.Models;
using ShockLink.API.Models.Requests;
using ShockLink.API.Utils;
using ShockLink.Common.Redis;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller.Account;

[ApiController]
[AllowAnonymous]
[Route("/{version:apiVersion}/account/login")]
public class LoginController : ShockLinkControllerBase
{
    private readonly ShockLinkContext _db;
    private readonly ILogger<LoginController> _logger;
    private readonly IRedisCollection<LoginSession> _loginSessions;

    public LoginController(ShockLinkContext db, ILogger<LoginController> logger, IRedisConnectionProvider provider)
    {
        _logger = logger;
        _loginSessions = provider.RedisCollection<LoginSession>(false);
        _db = db;
    }
    
    [HttpPost]
    public async Task<BaseResponse<LoginResponse>> Login(Login data)
    {
        var user = await _db.Users.SingleOrDefaultAsync(x => x.Email == data.Email);
        if (user == null || !SecurePasswordHasher.Verify(data.Password, user.Password))
        {
            _logger.LogInformation("Failed to authenticate with email [{Email}]", data.Email);
            return EBaseResponse<LoginResponse>("The provided credentials do not match any account",
                HttpStatusCode.Unauthorized);
        }

        if (!user.EmailActived) return EBaseResponse<LoginResponse>("You must activate your account first, before you can login",
                HttpStatusCode.Forbidden);
        
        var randomSessionId = CryptoUtils.RandomString(64);
        
        HttpContext.Response.Cookies.Append("ShockLinkSession", randomSessionId, new CookieOptions
        {
            Expires = new DateTimeOffset(DateTime.UtcNow.AddDays(7)),
            Secure = true,
            HttpOnly = true,
            SameSite = SameSiteMode.Strict
        });
        
        await _loginSessions.InsertAsync(new LoginSession
        {
            Id = randomSessionId,
            UserId = user.Id,
            UserAgent = HttpContext.Request.Headers.UserAgent.ToString(),
            Ip = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? string.Empty,
        }, TimeSpan.FromDays(7));
        
        return new BaseResponse<LoginResponse>
        {
            Message = "Successfully signed in",
            Data = new LoginResponse
            {
                ShockLinkSession = randomSessionId
            }
        };
    }
    
    public class LoginResponse
    {
        public required string ShockLinkSession { get; set; }
    }
}