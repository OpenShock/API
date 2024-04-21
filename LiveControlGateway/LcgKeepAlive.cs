using OpenShock.Common.Redis;
using OpenShock.Common.Utils;
using Redis.OM.Contracts;
using Redis.OM.Searching;

namespace OpenShock.LiveControlGateway;

/// <summary>
/// Lcg keep alive task, to report to redis
/// </summary>
public class LcgKeepAlive : IHostedService
{
    private readonly ILogger<LcgKeepAlive> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly IRedisConnectionProvider _redisConnectionProvider;
    private readonly IRedisCollection<LcgNode> _lcgNodes;
    
    private const uint KeepAliveInterval = 35; // 35 seconds
    
    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="redisConnectionProvider"></param>
    /// <param name="logger"></param>
    public LcgKeepAlive(IRedisConnectionProvider redisConnectionProvider, ILogger<LcgKeepAlive> logger, IWebHostEnvironment env)
    {
        _redisConnectionProvider = redisConnectionProvider;
        _logger = logger;
        _env = env;
        _lcgNodes = redisConnectionProvider.RedisCollection<LcgNode>(false);
    }

    private async Task SelfOnline()
    {
        var online = await _lcgNodes.FindByIdAsync(LCGGlobals.LCGConfig.Fqdn);
        if (online == null)
        {
            await _lcgNodes.InsertAsync(new LcgNode
            {
                Fqdn = LCGGlobals.LCGConfig.Fqdn,
                Country = LCGGlobals.LCGConfig.CountryCode,
                Load = 0,
                Environment = _env.EnvironmentName
            }, TimeSpan.FromSeconds(35));
            return;
        }

        if (online.Country != LCGGlobals.LCGConfig.CountryCode)
        {
            var changeTracker = _redisConnectionProvider.RedisCollection<LcgNode>();
            var tracked = await changeTracker.FindByIdAsync(LCGGlobals.LCGConfig.Fqdn);
            if (tracked != null)
            {
                tracked.Country = LCGGlobals.LCGConfig.CountryCode;
                await changeTracker.SaveAsync();
                _logger.LogInformation("Updated firmware version of online device");
            }
            else
                _logger.LogWarning(
                    "Could not save changed firmware version to redis, device was not found in change tracker, this shouldn't be possible but it somehow was?");
        }

        await _redisConnectionProvider.Connection.ExecuteAsync("EXPIRE",
            $"{typeof(LcgNode).FullName}:{LCGGlobals.LCGConfig.Fqdn}", KeepAliveInterval);
    }

    private async Task Loop()
    {
        while (true)
        {
            try
            {
                _logger.LogDebug("Sending keep alive...");
                await SelfOnline();
                await Task.Delay(15_000);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error in loop");
            }
        }
        // ReSharper disable once FunctionNeverReturns
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        LucTask.Run(Loop);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}