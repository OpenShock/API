using OpenShock.Common.Redis;

namespace OpenShock.Common.Services.Session;

public interface ISessionService
{
    public Task<CreateSessionResult> CreateSessionAsync(Guid userId, string userAgent, string ipAddress);

    public Task<IReadOnlyList<LoginSession>> ListSessionsByUserId(Guid userId);

    public Task<LoginSession?> GetSessionById(string sessionId);

    public Task<LoginSession?> GetSessionByPulbicId(Guid publicSessionId);

    public Task UpdateSession(LoginSession loginSession, TimeSpan ttl);

    public Task<bool> DeleteSessionById(string sessionId);

    public Task<bool> DeleteSessionByPublicId(Guid publicSessionId);

    public Task DeleteSession(LoginSession loginSession);
}

public sealed record CreateSessionResult(Guid Id, string Token);