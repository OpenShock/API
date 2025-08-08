using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Constants;
using OpenShock.Common.Redis;
using OpenShock.Common.Utils;
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

    public async Task<CreateSessionResult> CreateSessionAsync(Guid userId, string userAgent, string ipAddress)
    {
        Guid id = Guid.CreateVersion7();
        string token = CryptoUtils.RandomString(AuthConstants.GeneratedTokenLength);

        await _loginSessions.InsertAsync(new LoginSession
        {
            Id = HashingUtils.HashToken(token),
            UserId = userId,
            UserAgent = userAgent,
            Ip = ipAddress,
            PublicId = id,
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.Add(Duration.LoginSessionLifetime),
        }, Duration.LoginSessionLifetime);

        return new CreateSessionResult(id, token);
    }

    public async Task<IReadOnlyList<LoginSession>> ListSessionsByUserIdAsync(Guid userId)
    {
        return await _loginSessions.Where(x => x.UserId == userId).ToArrayAsync();
    }

    public async Task<LoginSession?> GetSessionByTokenAsync(string sessionToken)
    {
        // Only hash new tokens, old ones are 64 chars long
        if (sessionToken.Length == AuthConstants.GeneratedTokenLength)
        {
            sessionToken = HashingUtils.HashToken(sessionToken);
        }
        
        return await _loginSessions.FindByIdAsync(sessionToken);
    }

    public async Task<LoginSession?> GetSessionByIdAsync(Guid sessionId)
    {
        return await _loginSessions.FirstOrDefaultAsync(x => x.PublicId == sessionId);
    }

    public async Task UpdateSessionAsync(LoginSession session, TimeSpan ttl)
    {
        await _loginSessions.UpdateAsync(session, ttl);
    }

    public async Task<bool> DeleteSessionByTokenAsync(string sessionToken)
    {
        // Only hash new tokens, old ones are 64 chars long
        if (sessionToken.Length == AuthConstants.GeneratedTokenLength)
        {
            sessionToken = HashingUtils.HashToken(sessionToken);
        }
        
        var session = await _loginSessions.FindByIdAsync(sessionToken);
        if (session is null) return false;

        await _loginSessions.DeleteAsync(session);
        return true;
    }

    public async Task<bool> DeleteSessionByIdAsync(Guid sessionId)
    {
        var session = await _loginSessions.FirstOrDefaultAsync(x => x.PublicId == sessionId);
        if (session is null) return false;

        await _loginSessions.DeleteAsync(session);
        return true;
    }

    public async Task<int> DeleteSessionsByUserIdAsync(Guid userId)
    {
        var sessions = await _loginSessions.Where(x => x.UserId == userId).ToArrayAsync();

        await _loginSessions.DeleteAsync(sessions);

        return sessions.Length;
    }

    public async Task DeleteSessionAsync(LoginSession loginSession)
    {
        await _loginSessions.DeleteAsync(loginSession);
    }
}