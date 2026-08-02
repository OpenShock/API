using OpenShock.Common.Redis;
using OpenShock.Common.Utils;
using OpenShock.LiveControlGateway.Options;
using Redis.OM.Contracts;

namespace OpenShock.LiveControlGateway;

/// <summary>
/// Lcg keep alive task, to report to redis
/// </summary>
public sealed class LcgKeepAlive : BackgroundService
{
    private readonly IRedisConnectionProvider _redisConnectionProvider;
    private readonly IWebHostEnvironment _env;
    private readonly LcgOptions _options;
    private readonly ILogger<LcgKeepAlive> _logger;
    private readonly PeriodicTimer _periodicTimer = new(KeepAliveInterval);
    
    private uint _errorsInRow;
    
    private static readonly TimeSpan KeepAliveKeyTtl = TimeSpan.FromSeconds(35); // 35 seconds
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

        var nodeId = _options.GetPublicNodeId();

        var online = await lcgNodes.FindByIdAsync(nodeId);
        if (online is null)
        {
            await lcgNodes.InsertAsync(new LcgNode
            {
                Id = nodeId,
                Host = _options.Fqdn,
                Port = _options.PublicPort,
                PathPrefix = _options.NormalizedPublicPath,
                Country = _options.CountryCode,
                Load = 0,
                Environment = _env.EnvironmentName
            }, KeepAliveKeyTtl);
            return;
        }

        // TODO: Load reporting
        // Refresh whenever any advertised field drifted. This also backfills nodes written by an
        // older build: a default gateway keeps the same (bare-host) key across the upgrade, so the
        // pre-existing JSON is found here and would otherwise keep Host/Port/PathPrefix missing.
        if (online.Country != _options.CountryCode
            || online.Environment != _env.EnvironmentName
            || online.Host != _options.Fqdn
            || online.Port != _options.PublicPort
            || online.PathPrefix != _options.NormalizedPublicPath)
        {
            var changeTracker = _redisConnectionProvider.RedisCollection<LcgNode>();
            var tracked = await changeTracker.FindByIdAsync(nodeId);
            if (tracked is not null)
            {
                tracked.Host = _options.Fqdn;
                tracked.Port = _options.PublicPort;
                tracked.PathPrefix = _options.NormalizedPublicPath;
                tracked.Country = _options.CountryCode;
                tracked.Environment = _env.EnvironmentName;

                await changeTracker.SaveAsync();
                _logger.LogInformation("Updated keep alive key in redis {@NewKey}", tracked);
            }
            else
                _logger.LogWarning(
                    "Could not save changed firmware version to redis, device was not found in change tracker, this can only happen when our key expired between reads");
        }

        await _redisConnectionProvider.Connection.ExecuteAsync("EXPIRE",
            $"{typeof(LcgNode).FullName}:{nodeId}", (int)KeepAliveKeyTtl.TotalSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await _periodicTimer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                _logger.LogDebug("Sending keep alive...");
                await SelfOnline();
                _logger.LogDebug("Sent keep alive!");
                _errorsInRow = 0;
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
            }
        }
    }
}