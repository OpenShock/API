using System.Collections.Concurrent;
using System.Timers;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.OpenShockDb;
using Timer = System.Timers.Timer;

namespace OpenShock.ServicesCommon.Services.BatchUpdate;

public sealed class BatchUpdateService : IHostedService, IBatchUpdateService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);

    private readonly IDbContextFactory<OpenShockContext> _dbFactory;
    private readonly ILogger<BatchUpdateService> _logger;
    private readonly Timer _updateTimer;

    private readonly ConcurrentDictionary<Guid, bool> _tokenLastUpdated = new();

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
            if (keys.Length < 1) return;

            // Yeah
            foreach (var guid in keys) _tokenLastUpdated.TryRemove(guid, out _);
            
            await using var db = await _dbFactory.CreateDbContextAsync();
            await db.ApiTokens.Where(x => keys.Contains(x.Id))
                .ExecuteUpdateAsync(x => x.SetProperty(y => y.LastUsed, DateTime.UtcNow));

            await db.SaveChangesAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in batch update loop");
        }
    }

    public void UpdateTokenLastUsed(Guid tokenId)
    {
        _tokenLastUpdated[tokenId] = false;
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