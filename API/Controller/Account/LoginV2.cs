using Microsoft.AspNetCore.Mvc;
using OpenShock.API.Models.Requests;
using System.Net;
using Asp.Versioning;
using OpenShock.API.Services.Account;
using OpenShock.Common.Errors;
using OpenShock.Common.Problems;
using OpenShock.Common.Services.Turnstile;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    /// <summary>
    /// Authenticate a user
    /// </summary>
    /// <response code="200">User successfully logged in</response>
    /// <response code="401">Invalid username or password</response>
    [HttpPost("login")]
    [ProducesSuccess]
    [ProducesProblem(HttpStatusCode.Unauthorized, "InvalidCredentials")]
    [ProducesProblem(HttpStatusCode.Forbidden, "InvalidDomain")]
    [MapToApiVersion("2")]
    public async Task<IActionResult> LoginV2(
        [FromBody] LoginV2 body,
        [FromServices] IAccountService accountService,
        [FromServices] ICloudflareTurnstileService turnstileService,
        [FromServices] ApiConfig apiConfig,
        CancellationToken cancellationToken)
    {
        var cookieDomainToUse = apiConfig.Frontend.CookieDomain.Split(',').FirstOrDefault(domain => Request.Headers.Host.ToString().EndsWith(domain, StringComparison.OrdinalIgnoreCase));
        if (cookieDomainToUse == null) return Problem(LoginError.InvalidDomain);

        var turnStile = await turnstileService.VerifyUserResponseToken(body.TurnstileResponse, HttpContext.Connection.RemoteIpAddress, cancellationToken);
        if (!turnStile.IsT0) return Problem(TurnstileError.InvalidTurnstile);
            
        var loginAction = await accountService.Login(body.Email, body.Password, new LoginContext
        {
            Ip = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? string.Empty,
            UserAgent = HttpContext.Request.Headers.UserAgent.ToString()
        }, cancellationToken);

        if (loginAction.IsT1) return Problem(LoginError.InvalidCredentials);
        
        
        HttpContext.Response.Cookies.Append("openShockSession", loginAction.AsT0.Value, new CookieOptions
        {
            Expires = new DateTimeOffset(DateTime.UtcNow.Add(accountService.SessionLifetime)),
            Secure = true,
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Domain = "." + cookieDomainToUse
        });

        return RespondSuccessSimple("Successfully logged in");
    }
}