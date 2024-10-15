using OneOf;
using OneOf.Types;
using OpenShock.API.Models.Response;
using OpenShock.Common;
using OpenShock.Common.Authentication.Handlers;
using OpenShock.Common.Redis;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Searching;

namespace OpenShock.API.Services.Session;

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
        _loginSessions = redisConnectionProvider.RedisCollection<LoginSession>();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<LoginSessionResponse>> ListSessions(Guid userId)
    {
        var sessions = await _loginSessions.Where(x => x.UserId == userId).ToListAsync();

        var needsSave = false;
        foreach (var session in sessions)
        {
            if(LoginSessionAuthentication.UpdateOlderLoginSessions(session)) needsSave = true;
        }
        if(needsSave) await _loginSessions.SaveAsync();
        
        var sessionResponses = sessions.Select(x => new LoginSessionResponse
        {
            Ip = x.Ip,
            UserAgent = x.UserAgent,
            Id = x.PublicId!.Value,
            Created = x.Created!.Value,
            Expires = x.Expires!.Value
        });
        return sessionResponses;
    }

    /// <inheritdoc />
    public async Task<OneOf<Success, NotFound>> DeleteSession(Guid userId, Guid sessionId)
    {
        var session = await _loginSessions.Where(x => x.UserId == userId && x.PublicId == sessionId)
            .SingleOrDefaultAsync();
        if (session == null) return new NotFound();

        await _loginSessions.DeleteAsync(session);

        return new Success();
    }
    

}