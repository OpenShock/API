using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenShock.Common.Options;
using OpenShock.Common.Services.Session;
using OpenShock.Common.Utils;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    public async Task<IActionResult> Logout([FromServices] ISessionService sessionService)
    {
        // Remove session if valid
        if (HttpContext.TryGetUserSessionToken(out var sessionToken))
        {
            await sessionService.DeleteSessionByTokenAsync(sessionToken);
        }

        // Make sure cookie is removed, no matter if authenticated or not
        RemoveSessionKeyCookie();

        // its always a success, logout endpoints should be idempotent
        return Ok();
    }
}