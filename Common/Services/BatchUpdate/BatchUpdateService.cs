using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NRedisStack.RedisStackCommands;
using OpenShock.Common.Constants;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.Common.Services.ControlStats;
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

internal sealed class ConcurrentCountingBatchQueue<TKey> where TKey : notnull
{
    private readonly Lock _lock = new();
    private Dictionary<TKey, long> _dictionary = new();

    public void Increment(TKey key) => Add(key, 1);

    public void Add(TKey key, long count)
    {
        lock (_lock)
        {
            _dictionary[key] = _dictionary.GetValueOrDefault(key) + count;
        }
    }

    public Dictionary<TKey, long> DequeueAll()
    {
        lock (_lock)
        {
            var items = _dictionary;
            _dictionary = new Dictionary<TKey, long>();
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
    private readonly ConcurrentCountingBatchQueue<(Guid Id, string TokenHash)> _automationTokenUses = new();
    private readonly ControlStatsAccumulator _controlStats = new();

    /// <summary>
    /// Control stat batches not yet applied, oldest first. Only touched by the flush loop, a batch keeps its id
    /// across retries so it is applied at most once.
    /// </summary>
    private readonly Queue<PendingControlStatBatch> _controlStatBatches = new();

    /// <summary>
    /// Redis list of control stat batches an instance couldn't apply before shutting down, any instance applies them.
    /// </summary>
    private const string ParkedControlStatBatchesKey = "control-stats:parked-batches";

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
    
    private async Task UpdateAutomationTokens()
    {
        var uses = _automationTokenUses.DequeueAll();

        // Skip if there is nothing
        if (uses.Count < 1) return;

        var now = DateTime.UtcNow;

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            foreach (var ((automationTokenId, tokenHash), count) in uses.ToArray())
            {
                // Matching the hash drops uses of a secret rotated away since, so they never count towards
                // the new one. Rotation resets the counters, and this keeps them reset.
                await db.AutomationTokens.Where(x => x.Id == automationTokenId && x.TokenHash == tokenHash)
                    .ExecuteUpdateAsync(x => x
                        .SetProperty(y => y.LastUsedAt, now)
                        .SetProperty(y => y.UseCount, y => y.UseCount + count));

                uses.Remove((automationTokenId, tokenHash));
            }
        }
        catch
        {
            // Unlike a time of last use, a count can't be rebuilt by the next request, so put back what
            // wasn't written for the next flush to retry.
            foreach (var (key, count) in uses) _automationTokenUses.Add(key, count);
            throw;
        }
    }

    private async Task UpdateControlStats()
    {
        await ClaimParkedControlStatBatches();

        var deltas = _controlStats.DequeueAll();
        if (deltas.Length > 0) _controlStatBatches.Enqueue(new PendingControlStatBatch(Guid.CreateVersion7(), deltas));

        // A batch that throws stays queued with its id. If its commit actually went through, the retry finds the
        // batch marker and skips it instead of counting it twice.
        while (_controlStatBatches.TryPeek(out var batch))
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            await db.ApplyControlStatBatchAsync(batch.Id, batch.Deltas);
            _controlStatBatches.Dequeue();
        }
    }

    private async Task ClaimParkedControlStatBatches()
    {
        try
        {
            var parked = await _connectionMultiplexer.GetDatabase().ListLeftPopAsync(ParkedControlStatBatchesKey, 100);
            if (parked is null) return;

            foreach (var value in parked)
            {
                var batch = JsonSerializer.Deserialize<PendingControlStatBatch>(value.ToString());
                if (batch is not null) _controlStatBatches.Enqueue(batch);
            }
        }
        catch (Exception e)
        {
            // Left in Redis, the next flush tries again
            _logger.LogWarning(e, "Failed to claim parked control stat batches");
        }
    }

    /// <summary>
    /// Stats can't be rebuilt, so whatever the final flush couldn't apply is handed to Redis for the next instance
    /// instead of being dropped with this process.
    /// </summary>
    private async Task ParkUnappliedControlStatBatches()
    {
        var late = _controlStats.DequeueAll();
        if (late.Length > 0) _controlStatBatches.Enqueue(new PendingControlStatBatch(Guid.CreateVersion7(), late));
        if (_controlStatBatches.Count == 0) return;

        try
        {
            var values = _controlStatBatches.Select(b => (RedisValue)JsonSerializer.Serialize(b)).ToArray();
            await _connectionMultiplexer.GetDatabase().ListRightPushAsync(ParkedControlStatBatchesKey, values);
            _logger.LogWarning("Parked {Count} unapplied control stat batches in Redis for another instance", values.Length);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Lost {Count} control stat batches on shutdown, neither the database nor Redis took them",
                _controlStatBatches.Count);
        }
    }

    private sealed record PendingControlStatBatch(Guid Id, ControlStatDelta[] Deltas);

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
    
    public void UpdateAutomationTokenUsed(Guid automationTokenId, string tokenHash)
    {
        _automationTokenUses.Increment((automationTokenId, tokenHash));
    }

    public void RecordControlStat(ControlStatDelta delta)
    {
        _controlStats.Add(delta);
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
        await ParkUnappliedControlStatBatches();
    }

    private async Task Flush()
    {
        try
        {
            await Task.WhenAll(UpdateTokens(), UpdateAutomationTokens(), UpdateControlStats(), UpdateSessions());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in batch update loop");
        }
    }
}
