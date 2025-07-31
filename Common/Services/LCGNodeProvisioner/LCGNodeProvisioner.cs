using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Constants;
using OpenShock.Common.Geo;
using OpenShock.Common.Redis;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Searching;

namespace OpenShock.Common.Services.LCGNodeProvisioner;

public sealed class LCGNodeProvisioner : ILCGNodeProvisioner
{
    private readonly string _environmentName;
    private readonly IRedisCollection<LcgNode> _lcgNodes;
    private readonly ILogger<LCGNodeProvisioner> _logger;

    public LCGNodeProvisioner(IRedisConnectionProvider redisConnectionProvider, IWebHostEnvironment environment, ILogger<LCGNodeProvisioner> logger)
    {
        _environmentName = environment.EnvironmentName;
        _lcgNodes = redisConnectionProvider.RedisCollection<LcgNode>(false);
        _logger = logger;
    }

    public async Task<LcgNode?> GetOptimalNodeAsync()
    {
        var node = await _lcgNodes
            .OrderBy(x => x.Load)
            .FirstOrDefaultAsync(x => x.Environment == _environmentName);

        if (node is null) _logger.LogWarning("No LCG nodes available!");
        if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebug("LCG node provisioned: {@LcgNode}", node);

        return node;
    }

    public async Task<LcgNode?> GetOptimalNodeAsync(Alpha2CountryCode countryCode)
    {
        if (countryCode.IsUnknown())
        {
            _logger.LogInformation("Country code is unknown, getting optimal node without geo location information");
            return await GetOptimalNodeAsync(_environmentName);
        }

        var nodes = await _lcgNodes
            .Where(x => x.Environment == _environmentName)
            .ToArrayAsync();

        var node = nodes
            .OrderBy(x => DistanceLookup.TryGetDistanceBetween(x.Country, countryCode, out float distance) ? distance : Distance.DistanceToAndromedaGalaxyInKm) // Just a large number :3
            .ThenBy(x => x.Load)
            .FirstOrDefault();

        if (node is null) _logger.LogWarning("No LCG nodes available!");
        if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebug("LCG node provisioned: {@LcgNode}", node);

        return node;
    }
}