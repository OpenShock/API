using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.API.Models.Requests;
using OpenShock.API.Utils;
using OpenShock.Common.Models;
using OpenShock.Common.Redis;
using Redis.OM.Contracts;
using System.Net;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(30);

    /// <summary>
    /// Authenticate a user
    /// </summary>
    /// <response code="200">User successfully logged in</response>
    /// <response code="401">Invalid username or password</response>
    /// <response code="403">Account not activated</response>
    [HttpPost("login")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<BaseResponse<object>> Login([FromBody] Login body)
    {
        var loginSessions = _redis.RedisCollection<LoginSession>(false);

        var user = await _db.Users.SingleOrDefaultAsync(x => x.Email == body.Email.ToLowerInvariant());
        if (user == null || !SecurePasswordHasher.Verify(body.Password, user.Password))
        {
            _logger.LogInformation("Failed to authenticate with email [{Email}]", body.Email);
            return EBaseResponse<object>("The provided credentials do not match any account",
                HttpStatusCode.Unauthorized);
        }

        if (!user.EmailActived) return EBaseResponse<object>("You must activate your account first, before you can login",
                HttpStatusCode.Forbidden);

        var randomSessionId = CryptoUtils.RandomString(64);

        await loginSessions.InsertAsync(new LoginSession
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
            SameSite = SameSiteMode.Strict,
            Domain = "." + APIGlobals.ApiConfig.CookieDomain
        });

        return new BaseResponse<object>
        {
            Message = "Successfully signed in"
        };
    }
}