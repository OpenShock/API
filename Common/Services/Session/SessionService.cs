using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Constants;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.Common.Services.Audit;
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
    private readonly IAuditService _auditService;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="redisConnectionProvider"></param>
    /// <param name="auditService"></param>
    public SessionService(IRedisConnectionProvider redisConnectionProvider, IAuditService auditService)
    {
        _loginSessions = redisConnectionProvider.RedisCollection<LoginSession>(false);
        _auditService = auditService;
    }

    public async Task<CreateSessionResult> CreateSessionAsync(Guid userId, string userAgent, string ipAddress, Guid? actorId)
    {
        Guid id = Guid.CreateVersion7();
        string token = CryptoUtils.RandomAlphaNumericString(AuthConstants.GeneratedTokenLength);

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

        await _auditService.LogAsync(
            userId,
            action: AuditAction.Login,
            actorId: actorId ?? userId,
            metadata: new LoginMetadata(id)
        );

        return new CreateSessionResult(id, token);
    }

    public IAsyncEnumerable<LoginSession> ListSessionsByUserIdAsync(Guid userId)
    {
        return _loginSessions.Where(x => x.UserId == userId);
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
        var sessions = await _loginSessions.Where(x => x.UserId == userId).ToListAsync();

        await _loginSessions.DeleteAsync(sessions);

        return sessions.Count;
    }

    public async Task DeleteSessionAsync(LoginSession loginSession)
    {
        await _loginSessions.DeleteAsync(loginSession);
    }

    public async Task LogoutSessionAsync(LoginSession loginSession)
    {
        // Record the logout before destroying the session. The session delete is idempotent, so if
        // this throws the caller can safely retry; deleting first would leave the session gone while
        // the caller sees a failure and no audit trail.
        await _auditService.LogAsync(
            loginSession.UserId,
            action: AuditAction.Logout,
            actorId: loginSession.UserId
        );

        await _loginSessions.DeleteAsync(loginSession);
    }
}