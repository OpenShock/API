using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Services.RedisPubSub;
using OpenShock.Common.Utils;
using OpenShock.LiveControlGateway.Controllers;
using StackExchange.Redis;

namespace OpenShock.LiveControlGateway.PubSub;

/// <summary>
/// Per-gateway coordinator for API token update notifications.
/// Holds a single ref-counted redis subscription per token, shared by every live control connection on this
/// gateway that authenticated with it. When a token changes, the limits are fetched from the database once and
/// pushed to all those connections, instead of each connection querying the database on its own.
/// </summary>
public sealed class ApiTokenUpdateSubscriber
{
    private readonly IDbContextFactory<OpenShockContext> _dbContextFactory;
    private readonly ISubscriber _subscriber;
    private readonly ILogger<ApiTokenUpdateSubscriber> _logger;

    private readonly Dictionary<Guid, Subscription> _subscriptions = new();
    private readonly SemaphoreSlim _subscriptionsLock = new(1, 1);

    private sealed class Subscription
    {
        public required Action<RedisChannel, RedisValue> Handler { get; init; }
        public readonly HashSet<LiveControlController> Clients = [];
    }

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="connectionMultiplexer"></param>
    /// <param name="dbContextFactory"></param>
    /// <param name="logger"></param>
    public ApiTokenUpdateSubscriber(
        IConnectionMultiplexer connectionMultiplexer,
        IDbContextFactory<OpenShockContext> dbContextFactory,
        ILogger<ApiTokenUpdateSubscriber> logger)
    {
        _dbContextFactory = dbContextFactory;
        _subscriber = connectionMultiplexer.GetSubscriber();
        _logger = logger;
    }

    /// <summary>
    /// Register a live control connection as interested in updates to the given token.
    /// Subscribes to the token's channel if this is the first connection using it on this gateway.
    /// </summary>
    public async Task Register(LiveControlController client, Guid tokenId)
    {
        using (await _subscriptionsLock.LockAsyncScoped())
        {
            if (!_subscriptions.TryGetValue(tokenId, out var sub))
            {
                Action<RedisChannel, RedisValue> handler = (_, _) => OsTask.Run(() => OnTokenUpdate(tokenId));
                await _subscriber.SubscribeAsync(RedisChannels.ApiTokenUpdate(tokenId), handler);
                sub = new Subscription { Handler = handler };
                _subscriptions[tokenId] = sub;
            }

            sub.Clients.Add(client);
        }
    }

    /// <summary>
    /// Unregister a live control connection. Unsubscribes from the token's channel once no connection uses it anymore.
    /// </summary>
    public async Task Unregister(LiveControlController client, Guid tokenId)
    {
        using (await _subscriptionsLock.LockAsyncScoped())
        {
            if (_subscriptions.TryGetValue(tokenId, out var sub) && sub.Clients.Remove(client) && sub.Clients.Count == 0)
            {
                await _subscriber.UnsubscribeAsync(RedisChannels.ApiTokenUpdate(tokenId), sub.Handler);
                _subscriptions.Remove(tokenId);
            }
        }
    }

    private async Task OnTokenUpdate(Guid tokenId)
    {
        LiveControlController[] clients;
        using (await _subscriptionsLock.LockAsyncScoped())
        {
            if (!_subscriptions.TryGetValue(tokenId, out var sub)) return;
            clients = [.. sub.Clients];
        }

        if (clients.Length == 0) return;

        bool paused;
        ApiTokenControlLimits? limits;
        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();

            var token = await db.ApiTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == tokenId && (x.ValidUntil == null || x.ValidUntil >= DateTime.UtcNow));

            if (token is null)
            {
                // Token revoked/expired: deny all further control on these connections.
                _logger.LogDebug("API token [{TokenId}] is gone, denying control on {Count} live connection(s)",
                    tokenId, clients.Length);
                paused = true;
                limits = null;
            }
            else
            {
                paused = token.ShockerControlPaused;
                limits = ApiTokenControlLimits.FromToken(token);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error fetching api token [{TokenId}] limits for live control connections", tokenId);
            return;
        }

        foreach (var client in clients)
            client.ApplyTokenLimits(paused, limits);
    }
}