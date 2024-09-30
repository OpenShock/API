using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Redis;
using OpenShock.Common.Utils;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Searching;

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
        var nodes = await _lcgNodes.Where(x => x.Environment == environment).ToArrayAsync();

        LcgNode? node;

        // Don't bother ordering by distance if we dont know the position of the countryCode
        if (countryCode.IsUnknown())
        {
            node = nodes.OrderBy(x => x.Load).FirstOrDefault();
        }
        else
        {
            node = nodes.OrderBy(x => CountryCodeMapper.TryGetDistanceBetween(x.Country, countryCode, out double distance) ? distance : double.PositiveInfinity).ThenBy(x => x.Load).FirstOrDefault();
        }

        if (node == null) _logger.LogWarning("No LCG nodes available!");
        else if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebug("LCG node provisioned: {@LcgNode}", node);

        return node;
    }
}