using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Extensions;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Utils;
using OpenShock.Common.Models;
using OpenShock.Common.Services.Audit;
using OpenShock.Common.Services.Session;

namespace OpenShock.API.Controller.Account;

public sealed partial class AccountController
{
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    public async Task<IActionResult> Logout(
        [FromServices] ISessionService sessionService,
        [FromServices] IAuditService auditService)
    {
        if (HttpContext.TryGetUserSessionToken(out var sessionToken))
        {
            var session = await sessionService.GetSessionByTokenAsync(sessionToken);
            if (session is not null)
            {
                await sessionService.DeleteSessionAsync(session);
                await auditService.LogAsync(
                    session.UserId,
                    AuditAction.Logout,
                    ipAddress: HttpContext.GetRemoteIP(),
                    userAgent: HttpContext.GetUserAgent());
            }
        }

        // Make sure cookie is removed, no matter if authenticated or not
        RemoveSessionKeyCookie();

        // its always a success, logout endpoints should be idempotent
        return Ok();
    }
}
