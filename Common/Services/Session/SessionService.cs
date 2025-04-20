using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Constants;
using OpenShock.Common.Redis;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Searching;

namespace OpenShock.Common.Services.Session;

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
        _loginSessions = redisConnectionProvider.RedisCollection<LoginSession>(false);
    }

    public async Task<Guid> CreateSessionAsync(string sessionId, Guid userId, string userAgent, string ipAddress)
    {
        Guid publicId = Guid.CreateVersion7();

        await _loginSessions.InsertAsync(new LoginSession
        {
            Id = sessionId,
            UserId = userId,
            UserAgent = userAgent,
            Ip = ipAddress,
            PublicId = publicId,
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.Add(Duration.LoginSessionLifetime),
        }, Duration.LoginSessionLifetime);

        return publicId;
    }

    public async Task<IReadOnlyList<LoginSession>> ListSessionsByUserId(Guid userId)
    {
        return await _loginSessions.Where(x => x.UserId == userId).ToArrayAsync();
    }

    public async Task<LoginSession?> GetSessionById(string sessionId)
    {
        return await _loginSessions.FindByIdAsync(sessionId);
    }

    public async Task<LoginSession?> GetSessionByPulbicId(Guid publicSessionId)
    {
        return await _loginSessions.Where(x => x.PublicId == publicSessionId).FirstOrDefaultAsync();
    }

    public async Task UpdateSession(LoginSession session, TimeSpan ttl)
    {
        await _loginSessions.UpdateAsync(session, ttl);
    }

    public async Task<bool> DeleteSessionById(string sessionId)
    {
        var session = await _loginSessions.FindByIdAsync(sessionId);
        if (session == null) return false;

        await _loginSessions.DeleteAsync(session);
        return true;
    }

    public async Task<bool> DeleteSessionByPublicId(Guid publicSessionId)
    {
        var session = await _loginSessions.Where(x => x.PublicId == publicSessionId).FirstOrDefaultAsync();
        if (session == null) return false;

        await _loginSessions.DeleteAsync(session);
        return true;
    }

    public async Task DeleteSession(LoginSession loginSession)
    {
        await _loginSessions.DeleteAsync(loginSession);
    }
}