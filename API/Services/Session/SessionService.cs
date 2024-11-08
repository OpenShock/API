using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Authentication.Handlers;
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

    public async Task<IEnumerable<LoginSession>> ListSessionsByUserId(Guid userId)
    {
        var sessions = await _loginSessions.Where(x => x.UserId == userId).ToListAsync();

        var needsSave = false;
        foreach (var session in sessions)
        {
            if (LoginSessionAuthentication.UpdateOlderLoginSessions(session)) needsSave = true;
        }
        if (needsSave) await _loginSessions.SaveAsync();

        return sessions;
    }

    public async Task<LoginSession?> GetSessionByPulbicId(Guid publicSessionId)
    {
        return await _loginSessions.Where(x => x.PublicId == publicSessionId)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> DeleteSessionById(string sessionId)
    {
        int affected = await _loginSessions.Where(x => x.Id == sessionId).ExecuteDeleteAsync();
        return affected > 0;
    }

    public async Task<bool> DeleteSessionByPublicId(Guid publicSessionId)
    {
        int affected = await _loginSessions.Where(x => x.PublicId == publicSessionId).ExecuteDeleteAsync();
        return affected > 0;
    }

    public async Task DeleteSession(LoginSession loginSession)
    {
        await _loginSessions.DeleteAsync(loginSession);
    }
}