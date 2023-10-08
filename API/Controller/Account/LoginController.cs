using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Requests;
using OpenShock.API.Models.Response;
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
public class LoginController : OpenShockControllerBase
{
    private readonly OpenShockContext _db;
    private readonly ILogger<LoginController> _logger;
    private readonly IRedisCollection<LoginSession> _loginSessions;
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(30);

    public LoginController(OpenShockContext db, ILogger<LoginController> logger, IRedisConnectionProvider provider)
    {
        _logger = logger;
        _loginSessions = provider.RedisCollection<LoginSession>(false);
        _db = db;
    }
    
    [HttpPost]
    public async Task<BaseResponse<object>> Login(Login data)
    {
        var user = await _db.Users.SingleOrDefaultAsync(x => x.Email == data.Email.ToLowerInvariant());
        if (user == null || !SecurePasswordHasher.Verify(data.Password, user.Password))
        {
            _logger.LogInformation("Failed to authenticate with email [{Email}]", data.Email);
            return EBaseResponse<object>("The provided credentials do not match any account",
                HttpStatusCode.Unauthorized);
        }

        if (!user.EmailActived) return EBaseResponse<object>("You must activate your account first, before you can login",
                HttpStatusCode.Forbidden);
        
        var randomSessionId = CryptoUtils.RandomString(64);
        
        await _loginSessions.InsertAsync(new LoginSession
        {
            Id = randomSessionId,
            UserId = user.Id,
            UserAgent = HttpContext.Request.Headers.UserAgent.ToString(),
            Ip = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? string.Empty,
        }, SessionLifetime);
        
        HttpContext.Response.Cookies.Append("openShockSession", randomSessionId, new CookieOptions
        {
            Expires = new DateTimeOffset(DateTime.UtcNow.Add(SessionLifetime)),
            Secure = true,
            HttpOnly = true,
            SameSite = SameSiteMode.Strict
        });
        
        return new BaseResponse<object>
        {
            Message = "Successfully signed in"
        };
    }
}