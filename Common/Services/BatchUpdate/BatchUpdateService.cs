using Microsoft.EntityFrameworkCore;
using NRedisStack.RedisStackCommands;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.Common.Utils;
using StackExchange.Redis;

using OpenShock.Internal.Common.Utils;

namespace OpenShock.Common.Services.BatchUpdate;

internal sealed class ConcurrentUniqueBatchQueue<TKey, TValue> where TKey : notnull
{
    private readonly Lock _lock = new();
    private Dictionary<TKey, TValue> _dictionary = new();

    public void Enqueue(TKey key, TValue value)
    {
        lock (_lock)
        {
            _dictionary[key] = value;
        }
    }

    public Dictionary<TKey, TValue> DequeueAll()
    {
        lock (_lock)
        {
            var items = _dictionary;
            _dictionary = new Dictionary<TKey, TValue>();
            return items;
        }
    }
}

public sealed class BatchUpdateService : BackgroundService, IBatchUpdateService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);

    private readonly IDbContextFactory<OpenShockContext> _dbFactory;
    private readonly ILogger<BatchUpdateService> _logger;
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    private readonly ConcurrentUniqueBatchQueue<Guid, bool> _tokenLastUsed = new();
    private readonly ConcurrentUniqueBatchQueue<string, DateTimeOffset> _sessionLastUsed = new();

    public BatchUpdateService(IDbContextFactory<OpenShockContext> dbFactory, ILogger<BatchUpdateService> logger, IConnectionMultiplexer connectionMultiplexer)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _connectionMultiplexer = connectionMultiplexer;
    }

    private async Task UpdateTokens()
    {
        var keys = _tokenLastUsed.DequeueAll().Keys.ToArray();

        // Skip if there is nothing
        if (keys.Length < 1) return;
            
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.ApiTokens.Where(x => keys.Contains(x.Id))
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.LastUsed, DateTime.UtcNow));

        await db.SaveChangesAsync();
    }
    
    private async Task UpdateSessions()
    {
        var sessionsToUpdate = new List<Task<bool>>(); 
        
        var json = _connectionMultiplexer.GetDatabase().JSON();
        
        foreach (var (sessionToken, lastUsed) in _sessionLastUsed.DequeueAll())
        {
            sessionsToUpdate.Add(json.SetAsync(typeof(LoginSession).FullName + ":" + sessionToken, "LastUsed", lastUsed.ToUnixTimeMilliseconds(), When.Always));
        }

        try
        {
            await Task.WhenAll(sessionsToUpdate);
        } catch (Exception e)
        {
            _logger.LogTrace(e, "Error updating a sessions last used value");
        }
    }

    public void UpdateApiTokenLastUsed(Guid apiTokenId)
    {
        _tokenLastUsed.Enqueue(apiTokenId, false);
    }
    
    public void UpdateSessionLastUsed(string sessionToken, DateTimeOffset lastUsed)
    {
        // Only hash new tokens, old ones are 64 chars long
        if (sessionToken.Length == AuthConstants.GeneratedTokenLength)
        {
            sessionToken = HashingUtils.HashToken(sessionToken);
        }
        
        _sessionLastUsed.Enqueue(sessionToken, lastUsed);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var updateTimer = new PeriodicTimer(Interval);

        try
        {
            while (await updateTimer.WaitForNextTickAsync(stoppingToken))
            {
                await Flush();
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown requested, fall through to the final flush
        }

        // Drain what was queued since the last tick, otherwise every shutdown
        // silently loses up to Interval worth of last used updates
        await Flush();
    }

    private async Task Flush()
    {
        try
        {
            await Task.WhenAll(UpdateTokens(), UpdateSessions());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in batch update loop");
        }
    }
}
