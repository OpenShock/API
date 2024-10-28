using OneOf;
using OneOf.Types;
using OpenShock.API.Models.Response;
using OpenShock.Common.Redis;

namespace OpenShock.API.Services.Session;

public interface ISessionService
{
    public Task<IEnumerable<LoginSession>> ListSessions(Guid userId);

    public Task<LoginSession?> GetSession(Guid userId);

    public Task DeleteSession(LoginSession loginSession);
}