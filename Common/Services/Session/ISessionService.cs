using OpenShock.Common.Redis;

namespace OpenShock.Common.Services.Session;

public interface ISessionService
{
    public Task<CreateSessionResult> CreateSessionAsync(Guid userId, string userAgent, string ipAddress);

    public Task<IReadOnlyList<LoginSession>> ListSessionsByUserId(Guid userId);

    public Task<LoginSession?> GetSessionByToken(string sessionToken);

    public Task<LoginSession?> GetSessionById(Guid sessionId);

    public Task UpdateSession(LoginSession loginSession, TimeSpan ttl);

    public Task<bool> DeleteSessionByToken(string sessionToken);

    public Task<bool> DeleteSessionById(Guid sessionId);

    public Task DeleteSession(LoginSession loginSession);
}

public sealed record CreateSessionResult(Guid Id, string Token);