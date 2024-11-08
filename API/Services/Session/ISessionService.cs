using OpenShock.Common.Redis;

namespace OpenShock.API.Services.Session;

public interface ISessionService
{
    public Task<IEnumerable<LoginSession>> ListSessionsByUserId(Guid userId);

    public Task<LoginSession?> GetSessionByPulbicId(Guid publicSessionId);

    public Task<bool> DeleteSessionById(string sessionId);

    public Task<bool> DeleteSessionByPublicId(Guid publicSessionId);

    public Task DeleteSession(LoginSession loginSession);
}