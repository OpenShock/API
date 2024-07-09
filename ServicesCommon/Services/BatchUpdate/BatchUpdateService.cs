using System.Collections.Concurrent;
using System.Timers;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.OpenShockDb;
using Timer = System.Timers.Timer;

namespace OpenShock.ServicesCommon.Services.BatchUpdate;

public sealed class BatchUpdateService : IHostedService, IBatchUpdateService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

    private readonly IDbContextFactory<OpenShockContext> _dbFactory;
    private readonly ILogger<BatchUpdateService> _logger;
    private readonly Timer _updateTimer;

    private readonly ConcurrentDictionary<Guid, DateTime> _tokenLastUpdated = new();

    public BatchUpdateService(IDbContextFactory<OpenShockContext> dbFactory, ILogger<BatchUpdateService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;

        _updateTimer = new Timer(Interval);
        _updateTimer.Elapsed += UpdateTimerOnElapsed;
    }

    private async void UpdateTimerOnElapsed(object? sender, ElapsedEventArgs eventArgs)
    {
        try
        {
            var keys = _tokenLastUpdated.Keys.ToArray();
            
            // Skip if there is nothing
            if(keys.Length < 1) return;
            
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            foreach (var guid in keys)
            {
                if (!_tokenLastUpdated.TryRemove(guid, out var tokenUpdateTime)) continue;
                
                var token = new ApiToken
                {
                    Id = guid,
                };
                token.LastUsed = tokenUpdateTime;
                
            }

            var rows = await db.SaveChangesAsync();
            if (rows > 0)
            {
                _logger.LogTrace("Batch update executed {Rows}", rows);
                return;
            }
            
            _logger.LogWarning("Batch update loop did not modify any rows");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in batch update loop");
        }
    }

    public void UpdateTokenLastUsed(Guid tokenId, DateTime? lastUsed = null)
    {
        lastUsed ??= DateTime.UtcNow;
        _tokenLastUpdated.AddOrUpdate(tokenId, lastUsed.Value, (_, _) => lastUsed.Value);
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