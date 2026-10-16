using System.Net;
using OpenShock.Common.Redis;
using OpenShock.Common.Services.Geo;

namespace OpenShock.Common.Services.Session;

public interface ISessionService
{
    public Task<CreateSessionResult> CreateSessionAsync(Guid userId, string userAgent, IPAddress ipAddress, Guid? actorId, IpEnrichmentData? enrichment = null, CancellationToken cancellationToken = default);

    public IAsyncEnumerable<LoginSession> ListSessionsByUserIdAsync(Guid userId);

    public Task<LoginSession?> GetSessionByTokenAsync(string sessionToken);

    public Task<LoginSession?> GetSessionByIdAsync(Guid sessionId);

    public Task UpdateSessionAsync(LoginSession loginSession, TimeSpan ttl);

    public Task<bool> DeleteSessionByTokenAsync(string sessionToken);

    public Task<bool> DeleteSessionByIdAsync(Guid sessionId);

    public Task<int> DeleteSessionsByUserIdAsync(Guid userId);

    public Task DeleteSessionAsync(LoginSession loginSession);

    public Task LogoutSessionAsync(LoginSession loginSession, CancellationToken cancellationToken = default);
}

public sealed record CreateSessionResult(Guid Id, string Token);