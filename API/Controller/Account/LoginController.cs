using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Requests;
using OpenShock.API.Utils;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.ServicesCommon;
using Redis.OM.Contracts;
using Redis.OM.Searching;

namespace OpenShock.API.Controller.Account;

[ApiController]
[AllowAnonymous]
[Route("/{version:apiVersion}/account/login")]
public class LoginController : ShockLinkControllerBase
{
    private readonly OpenShockContext _db;
    private readonly ILogger<LoginController> _logger;
    private readonly IRedisCollection<LoginSession> _loginSessions;

    public LoginController(OpenShockContext db, ILogger<LoginController> logger, IRedisConnectionProvider provider)
    {
        _logger = logger;
        _loginSessions = provider.RedisCollection<LoginSession>(false);
        _db = db;
    }
    
    [HttpPost]
    public async Task<BaseResponse<LoginResponse>> Login(Login data)
    {
        var user = await _db.Users.SingleOrDefaultAsync(x => x.Email == data.Email.ToLowerInvariant());
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
            Expires = new DateTimeOffset(DateTime.UtcNow.AddDays(30)),
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
        }, TimeSpan.FromDays(30));
        
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