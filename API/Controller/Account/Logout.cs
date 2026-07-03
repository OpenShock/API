using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Extensions;
using OpenShock.Common.Services.Session;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    public async Task<IActionResult> Logout(
        [FromServices] ISessionService sessionService)
    {
        if (HttpContext.TryGetUserSessionToken(out var sessionToken))
        {
            var session = await sessionService.GetSessionByTokenAsync(sessionToken);
            if (session is not null)
            {
                await sessionService.LogoutSessionAsync(session);
            }
        }

        // Make sure cookie is removed, no matter if authenticated or not
        RemoveSessionKeyCookie();

        // its always a success, logout endpoints should be idempotent
        return Ok();
    }
}
