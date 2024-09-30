using Microsoft.EntityFrameworkCore;
using OpenShock.Common;
using OpenShock.Common.Redis;
using OpenShock.Common.Utils;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using System.Linq;

namespace OpenShock.ServicesCommon.Geo;

public sealed class GeoLocation : IGeoLocation
{
    private readonly ILogger<GeoLocation> _logger;
    private readonly IRedisCollection<LcgNode> _lcgNodes;
    
    public GeoLocation(IRedisConnectionProvider redisConnectionProvider, ILogger<GeoLocation> logger)
    {
        _logger = logger;
        _lcgNodes = redisConnectionProvider.RedisCollection<LcgNode>(false);
    }

    public async Task<LcgNode?> GetClosestNode(CountryCodeMapper.Alpha2CountryCode countryCode, string environment = "Production")
    {
        LcgNode? node = null;

        if (countryCode.IsUnknown())
        {
            node = await _lcgNodes
                .Where(x => x.Environment == environment)
                .OrderBy(x => x.Load)
                .FirstOrDefaultAsync();
        }
        else
        {
            var nodes = await _lcgNodes
                .Where(x => x.Environment == environment)
                .ToArrayAsync();

            node = nodes
                .OrderBy(x => CountryCodeMapper.TryGetDistanceBetween(x.Country, countryCode, out double distance) ? distance : Constants.DistanceToAndromedaGalaxyInKm) // Just a large number :3
                .ThenBy(x => x.Load)
                .FirstOrDefault();
        }

        if (node == null)
        {
            _logger.LogWarning("No LCG nodes available!");
            return null;
        }
        
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("LCG node provisioned: {@LcgNode}", node);
        }

        return node;
    }
}