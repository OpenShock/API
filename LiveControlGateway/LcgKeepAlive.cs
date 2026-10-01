using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenShock.Common.HealthChecks;
using OpenShock.Common.Redis;
using OpenShock.LiveControlGateway.Options;
using Redis.OM;
using Redis.OM.Contracts;

using OpenShock.Internal.Common.Utils;

namespace OpenShock.LiveControlGateway;

/// <summary>
/// Lcg keep alive task, to report to redis
/// </summary>
public sealed class LcgKeepAlive : BackgroundService
{
    private readonly IRedisConnectionProvider _redisConnectionProvider;
    private readonly IWebHostEnvironment _env;
    private readonly LcgOptions _options;
    private readonly HealthCheckService _healthCheckService;
    private readonly GatewayLoadSampler _loadSampler;
    private readonly ILogger<LcgKeepAlive> _logger;

    private uint _errorsInRow;
    private bool _healthy = true;

    private static readonly TimeSpan KeepAliveKeyTtl = TimeSpan.FromSeconds(35); // 35 seconds
    private static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(15); // 15 seconds

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="redisConnectionProvider"></param>
    /// <param name="env"></param>
    /// <param name="options"></param>
    /// <param name="healthCheckService"></param>
    /// <param name="loadSampler"></param>
    /// <param name="logger"></param>
    public LcgKeepAlive(IRedisConnectionProvider redisConnectionProvider, IWebHostEnvironment env, LcgOptions options, HealthCheckService healthCheckService, GatewayLoadSampler loadSampler, ILogger<LcgKeepAlive> logger)
    {
        _redisConnectionProvider = redisConnectionProvider;
        _env = env;
        _options = options;
        _healthCheckService = healthCheckService;
        _loadSampler = loadSampler;
        _logger = logger;
    }

    private async Task SelfOnline()
    {
        // Overwrite the whole node on every beat instead of diffing it against what Redis holds: the
        // write costs the same round trip the read did, a new advertised field can't be forgotten in a
        // comparison, and nodes left by an older build (same bare-host key, missing fields) get fixed
        // up for free. The TTL is set in the same call.
        var load = _loadSampler.Measure();

        await _redisConnectionProvider.RedisCollection<LcgNode>(false).InsertAsync(new LcgNode
        {
            Id = _options.GetPublicNodeId(),
            Host = _options.Fqdn,
            Port = _options.PublicPort,
            PathPrefix = _options.NormalizedPublicPath,
            Country = _options.CountryCode,
            Latitude = _options.Latitude,
            Longitude = _options.Longitude,
            Load = load,
            Environment = _env.EnvironmentName
        }, WhenKey.Always, KeepAliveKeyTtl);
    }

    /// <summary>
    /// Runs the readiness checks (Postgres, Redis) that back the readiness endpoint. A gateway that
    /// cannot reach them cannot authenticate a hub or serve live control, so it has no business being
    /// advertised as an available node.
    /// </summary>
    private async Task<bool> IsReadyAsync(CancellationToken cancellationToken)
    {
        HealthReport report;
        try
        {
            report = await _healthCheckService.CheckHealthAsync(
                registration => registration.Tags.Contains(HealthCheckTags.Ready), cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _healthy = false;
            _logger.LogError(e, "Readiness check failed to run, not announcing this gateway");
            return false;
        }

        if (report.Status == HealthStatus.Healthy)
        {
            if (!_healthy)
            {
                _healthy = true;
                _logger.LogInformation("Gateway is healthy again, resuming keep alive");
            }

            return true;
        }

        var failing = report.Entries
            .Where(entry => entry.Value.Status != HealthStatus.Healthy)
            .Select(entry => entry.Key);

        _healthy = false;
        _logger.LogWarning(
            "Gateway is not ready ({Failing}), not refreshing the keep alive key. It expires in at most {Ttl}, after which the API stops assigning hubs to this node",
            string.Join(", ", failing), KeepAliveKeyTtl);

        return false;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(KeepAliveInterval);

        try
        {
            do
            {
                // Gate the announce, not just the first one: letting the key lapse is how an already
                // online gateway takes itself out of rotation when a dependency drops. Skipping the
                // refresh (rather than deleting the key) means a blip shorter than the TTL costs nothing.
                if (!await IsReadyAsync(stoppingToken)) continue;

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
                    if (_errorsInRow >= 10)
                    {
                        _logger.LogCritical("Too many errors in a row sending keep alive, terminating process");
                        Environment.Exit(1001);
                    }
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Shutdown requested
        }
    }
}