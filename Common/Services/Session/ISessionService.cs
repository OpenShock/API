using OpenShock.Common.Redis;

namespace OpenShock.Common.Services.Session;

public interface ISessionService
{
    public Task<CreateSessionResult> CreateSessionAsync(Guid userId, string userAgent, string ipAddress);

    public Task<IReadOnlyList<LoginSession>> ListSessionsByUserIdAsync(Guid userId);

    public Task<LoginSession?> GetSessionByTokenAsync(string sessionToken);

    public Task<LoginSession?> GetSessionByIdAsync(Guid sessionId);

    public Task UpdateSessionAsync(LoginSession loginSession, TimeSpan ttl);

    public Task<bool> DeleteSessionByTokenAsync(string sessionToken);

    public Task<bool> DeleteSessionByIdAsync(Guid sessionId);

    public Task DeleteSessionAsync(LoginSession loginSession);
}

public sealed record CreateSessionResult(Guid Id, string Token);