using Microsoft.Extensions.Caching.Distributed;
using System.Security.Cryptography;
using System.Text.Json;

namespace OpenShock.API.OAuth.FlowStore;

public sealed class CacheOAuthFlowStore(IDistributedCache cache) : IOAuthFlowStore
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);
    private static string Key(string id) => $"oauth:flow:{id}";

    public async Task<string> SaveAsync(OAuthSnapshot snap, TimeSpan ttl, CancellationToken ct = default)
    {
        var id = Convert.ToBase64String(RandomNumberGenerator.GetBytes(18))
                         .TrimEnd('=').Replace('+', '-').Replace('/', '_'); // url-safe
        var json = JsonSerializer.Serialize(snap, JsonOpts);
        await cache.SetStringAsync(Key(id), json, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, ct);
        return id;
    }

    public async Task<OAuthSnapshot?> GetAsync(string flowId, CancellationToken ct = default)
    {
        var json = await cache.GetStringAsync(Key(flowId), ct);
        return json is null ? null : JsonSerializer.Deserialize<OAuthSnapshot>(json, JsonOpts);
    }

    public Task DeleteAsync(string flowId, CancellationToken ct = default)
        => cache.RemoveAsync(Key(flowId), ct);
}