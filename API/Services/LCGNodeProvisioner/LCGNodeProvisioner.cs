using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Constants;
using OpenShock.Common.Geo;
using OpenShock.Common.Redis;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Searching;

namespace OpenShock.API.Services.LCGNodeProvisioner;

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
            return await GetOptimalNodeAsync();
        }

        // Load all nodes for our environment
        var nodes = await _lcgNodes
            .Where(x => x.Environment == _environmentName)
            .ToArrayAsync();
        
        if(nodes.Length < 1)
        {
            _logger.LogWarning("No LCG nodes available after filtering by environment [{Environment}]!", _environmentName);
            return null;
        }
        
        // Precompute distances
        var withDistances = nodes
            .Select(x => new
            {
                Node = x,
                Distance = DistanceLookup.TryGetDistanceBetween(x.Country, countryCode, out var dist) ? dist : Distance.DistanceToAndromedaGalaxyInKm
            })
            .ToArray();
        
        // 1) Find the closest region (min distance)
        var minDistance = withDistances.Min(x => x.Distance);

        var closestRegionNodes = withDistances
            .Where(x => Math.Abs(x.Distance - minDistance) < 1)
            .Select(x => x.Node)
            .ToArray();

        // 2) Among those, find minimal load
        var minLoad = closestRegionNodes.Min(x => x.Load);
        var loadCandidates = closestRegionNodes
            .Where(x => x.Load == minLoad)
            .ToArray();
        
        if(loadCandidates.Length < 1)
        {
            _logger.LogWarning("No LCG nodes available after filtering by geo location and load!");
            return null;
        }

        // 3) Randomly pick one of the tied nodes
        var node = loadCandidates[Random.Shared.Next(loadCandidates.Length)];
        
        if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebug("LCG node provisioned: {@LcgNode}", node);

        return node;
    }
}