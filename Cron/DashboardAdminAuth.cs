using Hangfire.Dashboard;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.Common.Utils;
using Redis.OM.Contracts;
using Redis.OM.Searching;

namespace OpenShock.Cron;

public sealed class DashboardAdminAuth : IDashboardAsyncAuthorizationFilter
{
    
    public async Task<bool> AuthorizeAsync(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var redis = httpContext.RequestServices.GetRequiredService<IRedisConnectionProvider>();
        var userSessions = redis.RedisCollection<LoginSession>(false);
        var db = httpContext.RequestServices.GetRequiredService<OpenShockContext>();

        if (httpContext.TryGetUserSession(out var userSessionCookie))
        {
            if (await SessionAuthAdmin(userSessionCookie, userSessions, db))
            {
                return true;
            }
        }

        await context.Response.WriteAsync("Unauthorized, you need to be authenticated as admin to access this page.");
        
        return false;
    }
    
    private static async Task<bool> SessionAuthAdmin(string sessionKey, IRedisCollection<LoginSession> loginSessions, OpenShockContext db)
    {
        var session = await loginSessions.FindByIdAsync(sessionKey);
        if (session == null) return false;
        var retrievedUser = await db.Users.FirstAsync(user => user.Id == session.UserId);
        return retrievedUser.Rank == RankType.Admin;
    }
}