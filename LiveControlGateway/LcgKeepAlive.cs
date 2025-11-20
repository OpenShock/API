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
    private readonly IServiceProvider _serviceProvider;
    private readonly IWebHostEnvironment _env;
    private readonly LcgOptions _options;
    private readonly ILogger<LcgKeepAlive> _logger;
    private readonly System.Timers.Timer _timer;
    
    private uint _errorsInRow;
    
    // ReSharper disable once InconsistentNaming
    private static readonly TimeSpan KeepAliveKeyTTL = TimeSpan.FromSeconds(35); // 35 seconds
    private static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(15); // 15 seconds

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="env"></param>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    public LcgKeepAlive(IServiceProvider serviceProvider, IWebHostEnvironment env, LcgOptions options, ILogger<LcgKeepAlive> logger)
    {
        _serviceProvider = serviceProvider;
        _env = env;
        _options = options;
        _logger = logger;
        
        _timer = new System.Timers.Timer(KeepAliveInterval)
        {
            AutoReset = true
        };
        _timer.Elapsed += OnTimerElapsed;
    }

    private async Task SelfOnline()
    {
        var redisConnectionProvider = _serviceProvider.GetRequiredService<IRedisConnectionProvider>();
        var lcgNodes = redisConnectionProvider.RedisCollection<LcgNode>(false);
        
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
            var changeTracker = redisConnectionProvider.RedisCollection<LcgNode>();
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

        await redisConnectionProvider.Connection.ExecuteAsync("EXPIRE",
            $"{typeof(LcgNode).FullName}:{_options.Fqdn}", KeepAliveKeyTTL);
    }

    private async void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            _logger.LogDebug("Sending keep alive...");
            await SelfOnline();
            _errorsInRow = 0;
        }
        catch (Exception ex)
        {
            
            _logger.LogError(ex, "Error sending keep alive");
            if(++_errorsInRow >= 10)
            {
                _logger.LogCritical("Too many errors in a row sending keep alive, terminating process");
                Environment.Exit(1001);
            }
        }
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer.Start();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.Stop();
        return Task.CompletedTask;
    }
}