using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Geo;
using OpenShock.Common.Redis;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Searching;

namespace OpenShock.Common.Services.LCGNodeProvisioner;

public sealed class LCGNodeProvisioner : ILCGNodeProvisioner
{
    private readonly ILogger<LCGNodeProvisioner> _logger;
    private readonly IRedisCollection<LcgNode> _lcgNodes;

    public LCGNodeProvisioner(IRedisConnectionProvider redisConnectionProvider, ILogger<LCGNodeProvisioner> logger)
    {
        _logger = logger;
        _lcgNodes = redisConnectionProvider.RedisCollection<LcgNode>(false);
    }

    public async Task<LcgNode?> GetOptimalNode(string environment)
    {
        var node = await _lcgNodes
            .Where(x => x.Environment == environment)
            .OrderBy(x => x.Load)
            .FirstOrDefaultAsync();

        if (node == null) _logger.LogWarning("No LCG nodes available!");
        if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebug("LCG node provisioned: {@LcgNode}", node);

        return node;
    }

    public async Task<LcgNode?> GetOptimalNode(Alpha2CountryCode countryCode, string environment)
    {
        if (countryCode.IsUnknown())
        {
            _logger.LogInformation("Country code is unknown, getting optimal node without geo location information");
            return await GetOptimalNode(environment);
        }

        var nodes = await _lcgNodes
            .Where(x => x.Environment == environment)
            .ToArrayAsync();

        var node = nodes
            .OrderBy(x => DistanceLookup.TryGetDistanceBetween(x.Country, countryCode, out float distance) ? distance : Constants.DistanceToAndromedaGalaxyInKm) // Just a large number :3
            .ThenBy(x => x.Load)
            .FirstOrDefault();

        if (node == null) _logger.LogWarning("No LCG nodes available!");
        if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebug("LCG node provisioned: {@LcgNode}", node);

        return node;
    }
}