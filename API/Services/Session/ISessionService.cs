using OneOf;
using OneOf.Types;
using OpenShock.API.Models.Response;
using OpenShock.Common.Redis;

namespace OpenShock.API.Services.Session;

public interface ISessionService
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public Task<IEnumerable<LoginSessionResponse>> ListSessions(Guid userId);
    
    public Task<OneOf<Success, NotFound>> DeleteSession(Guid userId, Guid sessionId);
}