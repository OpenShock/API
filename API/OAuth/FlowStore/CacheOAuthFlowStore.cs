using OpenShock.API.Controller.OAuth;
using OpenShock.Common.Authentication;
using OpenShock.Common.Utils;
using Redis.OM.Contracts;
using Redis.OM.Searching;

namespace OpenShock.API.OAuth.FlowStore;

public sealed class CacheOAuthFlowStore : IOAuthFlowStore
{
    private readonly IRedisCollection<OAuthSnapshot> _cache;

    public CacheOAuthFlowStore(IRedisConnectionProvider redisConnectionProvider)
    {
        _cache = redisConnectionProvider.RedisCollection<OAuthSnapshot>();
    }

    public async Task<string> SaveAsync(string provider, string externalId, string? email, string? displayName, DateTimeOffset issuedUtc)
    {
        var id = CryptoUtils.RandomString(32);
        
        var snap = new OAuthSnapshot
        {
            FlowId = id,
            Provider = provider,
            ExternalId = externalId,
            DisplayName = displayName,
            Email = email,
            IssuedUtc = issuedUtc
        };
        
        await _cache.InsertAsync(snap, OAuthConstants.StateLifetime);
        
        return id;
    }

    public async Task<OAuthSnapshot?> GetAsync(string flowId)
    {
        return await _cache.FindByIdAsync(flowId);
    }

    public async Task DeleteAsync(string flowId)
    {
        var snapshot = await _cache.FindByIdAsync(flowId);
        if (snapshot is not null)
        {
            await _cache.DeleteAsync(snapshot);
        }
    }
}