using OpenShock.Common.Redis;
using OpenShock.Common.Utils;
using OpenShock.LiveControlGateway.Options;
using Redis.OM.Contracts;

namespace OpenShock.LiveControlGateway;

/// <summary>
/// Lcg keep alive task, to report to redis
/// </summary>
public sealed class LcgKeepAlive : IHostedService
{
    private readonly IRedisConnectionProvider _redisConnectionProvider;
    private readonly IWebHostEnvironment _env;
    private readonly LcgOptions _options;
    private readonly ILogger<LcgKeepAlive> _logger;
    
    private uint _errorsInRow;
    
    private static readonly TimeSpan KeepAliveKeyTTL = TimeSpan.FromSeconds(35); // 35 seconds
    private static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(15); // 15 seconds

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="redisConnectionProvider"></param>
    /// <param name="env"></param>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    public LcgKeepAlive(IRedisConnectionProvider redisConnectionProvider, IWebHostEnvironment env, LcgOptions options, ILogger<LcgKeepAlive> logger)
    {
        _redisConnectionProvider = redisConnectionProvider;
        _env = env;
        _options = options;
        _logger = logger;
    }

    private async Task SelfOnline()
    {
        var lcgNodes = _redisConnectionProvider.RedisCollection<LcgNode>(false);
        
        var online = await lcgNodes.FindByIdAsync(_options.Fqdn);
        if (online is null)
        {
            await lcgNodes.InsertAsync(new LcgNode
            {
                Fqdn = _options.Fqdn,
                Country = _options.CountryCode,
                Load = 0,
                Environment = _env.EnvironmentName
            }, KeepAliveKeyTTL);
            return;
        }

        if (online.Country != _options.CountryCode)
        {
            var changeTracker = _redisConnectionProvider.RedisCollection<LcgNode>();
            var tracked = await changeTracker.FindByIdAsync(_options.Fqdn);
            if (tracked is not null)
            {
                tracked.Country = _options.CountryCode;
                await changeTracker.SaveAsync();
                _logger.LogInformation("Updated firmware version of online device");
            }
            else
                _logger.LogWarning(
                    "Could not save changed firmware version to redis, device was not found in change tracker, this shouldn't be possible but it somehow was?");
        }

        await _redisConnectionProvider.Connection.ExecuteAsync("EXPIRE",
            $"{typeof(LcgNode).FullName}:{_options.Fqdn}", (int)KeepAliveKeyTTL.TotalSeconds);
    }

    private async Task Loop()
    {
        while (true)
        {
            try
            {
                _logger.LogDebug("Sending keep alive...");
                await SelfOnline();
                _logger.LogDebug("Sent keep alive!");
                _errorsInRow = 0;
                await Task.Delay(KeepAliveInterval);
            }
            catch (Exception e)
            {
                ++_errorsInRow;
                _logger.LogError(e, "Error sending gateway keep alive {Attempt}", _errorsInRow);
                if(_errorsInRow >= 10)
                {
                    _logger.LogCritical("Too many errors in a row sending keep alive, terminating process");
                    Environment.Exit(1001);
                }
                
                await Task.Delay(KeepAliveInterval);
            }
        }
        // ReSharper disable once FunctionNeverReturns
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        OsTask.Run(Loop);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}