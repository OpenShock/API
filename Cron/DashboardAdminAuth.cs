using Hangfire.Dashboard;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.Session;

namespace OpenShock.Cron;

public sealed class DashboardAdminAuth : IDashboardAsyncAuthorizationFilter
{
    
    public async Task<bool> AuthorizeAsync(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var sessionService = httpContext.RequestServices.GetRequiredService<ISessionService>();
        var db = httpContext.RequestServices.GetRequiredService<OpenShockContext>();

        if (httpContext.TryGetUserSessionToken(out var userSessionCookie))
        {
            if (await SessionAuthAdmin(userSessionCookie, sessionService, db, httpContext.RequestAborted))
            {
                return true;
            }
        }

        await context.Response.WriteAsync("Unauthorized, you need to be authenticated as admin to access this page.");
        
        return false;
    }
    
    private static async Task<bool> SessionAuthAdmin(string sessionKey, ISessionService sessionService, OpenShockContext db, CancellationToken cancellationToken)
    {
        var session = await sessionService.GetSessionByTokenAsync(sessionKey);
        if (session is null) return false;

        var retrievedUser = await db.Users.FirstAsync(user => user.Id == session.UserId, cancellationToken);
        return retrievedUser.IsRole(RoleType.Admin);
    }
}