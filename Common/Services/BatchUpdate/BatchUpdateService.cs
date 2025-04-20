﻿using System.Collections.Concurrent;
using System.Timers;
using Microsoft.EntityFrameworkCore;
using NRedisStack.RedisStackCommands;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using StackExchange.Redis;
using Timer = System.Timers.Timer;

namespace OpenShock.Common.Services.BatchUpdate;

public sealed class BatchUpdateService : IHostedService, IBatchUpdateService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);

    private readonly IDbContextFactory<OpenShockContext> _dbFactory;
    private readonly ILogger<BatchUpdateService> _logger;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly Timer _updateTimer;

    private readonly ConcurrentDictionary<Guid, bool> _tokenLastUsed = new();
    private readonly ConcurrentQueue<SessionLastUsedUpdate> _sessionLastUsed = new();
    
    private sealed record SessionLastUsedUpdate(string SessionKey, DateTimeOffset LastUsed);

    public BatchUpdateService(IDbContextFactory<OpenShockContext> dbFactory, ILogger<BatchUpdateService> logger, IConnectionMultiplexer connectionMultiplexer)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _connectionMultiplexer = connectionMultiplexer;

        _updateTimer = new Timer(Interval);
        _updateTimer.Elapsed += UpdateTimerOnElapsed;
    }

    private async void UpdateTimerOnElapsed(object? sender, ElapsedEventArgs eventArgs)
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

    private async Task UpdateTokens()
    {
        var keys = _tokenLastUsed.Keys.ToArray();

        // Skip if there is nothing
        if (keys.Length < 1) return;

        // Yeah
        foreach (var guid in keys) _tokenLastUsed.TryRemove(guid, out _);
            
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.ApiTokens.Where(x => keys.Contains(x.Id))
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.LastUsed, DateTime.UtcNow));

        await db.SaveChangesAsync();
    }
    
    private async Task UpdateSessions()
    {
        var sessionsToUpdate = new List<Task<bool>>(); 
        
        var json = _connectionMultiplexer.GetDatabase().JSON();
        
        while (_sessionLastUsed.TryDequeue(out var update))
        {
            sessionsToUpdate.Add(json.SetAsync(typeof(LoginSession).FullName + ":" + update.SessionKey, "LastUsed", update.LastUsed.ToUnixTimeMilliseconds(), When.Always));
        }

        try
        {
            await Task.WhenAll(sessionsToUpdate);
        } catch (Exception e)
        {
            _logger.LogTrace(e, "Error updating a sessions last used value");
        }
    }

    public void UpdateTokenLastUsed(Guid tokenId)
    {
        _tokenLastUsed[tokenId] = false;
    }
    
    public void UpdateSessionLastUsed(string sessionKey, DateTimeOffset lastUsed)
    {
        _sessionLastUsed.Enqueue(new SessionLastUsedUpdate(sessionKey, lastUsed));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _updateTimer.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _updateTimer.Stop();
        return Task.CompletedTask;
    }
}