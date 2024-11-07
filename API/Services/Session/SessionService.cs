using OneOf;
using OneOf.Types;
using OpenShock.API.Models.Response;
using OpenShock.Common;
using OpenShock.Common.Authentication.Handlers;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Searching;

namespace OpenShock.API.Services.Session;

/// <summary>
/// Default implementation of ISessionService
/// </summary>
public sealed class SessionService : ISessionService
{
    private readonly IRedisCollection<LoginSession> _loginSessions;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="redisConnectionProvider"></param>
    public SessionService(IRedisConnectionProvider redisConnectionProvider)
    {
        _loginSessions = redisConnectionProvider.RedisCollection<LoginSession>();
    }

    public async Task<IEnumerable<LoginSession>> ListSessions(Guid userId)
    {
        var sessions = await _loginSessions.Where(x => x.UserId == userId).ToListAsync();

        var needsSave = false;
        foreach (var session in sessions)
        {
            if(LoginSessionAuthentication.UpdateOlderLoginSessions(session)) needsSave = true;
        }
        if(needsSave) await _loginSessions.SaveAsync();
        
        return sessions;
    }

    public async Task<LoginSession?> GetSession(Guid sessionId)
    {
        return await _loginSessions.Where(x => x.PublicId == sessionId)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> DeleteSession(Guid sessionId)
    {
        var session = await GetSession(sessionId);
        if (session == null) return false;
        
        await _loginSessions.DeleteAsync(session);
        return true;
    }

    public async Task DeleteSession(LoginSession loginSession)
    {
        await _loginSessions.DeleteAsync(loginSession);
    }
}