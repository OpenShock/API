using OpenShock.Common.Redis;
using OpenShock.Common.Utils;
using Redis.OM.Contracts;
using Redis.OM.Searching;

namespace OpenShock.ServicesCommon.Geo;

public class GeoLocation : IGeoLocation
{
    private readonly ILogger<GeoLocation> _logger;
    private readonly IRedisCollection<LcgNode> _lcgNodes;
    
    public GeoLocation(IRedisConnectionProvider redisConnectionProvider, ILogger<GeoLocation> logger)
    {
        _logger = logger;
        _lcgNodes = redisConnectionProvider.RedisCollection<LcgNode>(false);
    }
    
    public async Task<LcgNode?> GetClosestNode(CountryCodeMapper.CountryInfo country)
    {
        var nodes = await _lcgNodes.ToListAsync();
        var orderedNodes = nodes
            .OrderBy(x => CountryCodeMapper.GetCountryOrDefault(x.Country).DistanceTo(country))
            .ThenBy(x => x.Load);

        var node = orderedNodes.FirstOrDefault();
        if(node == null) _logger.LogWarning("No LCG nodes available!");
        else if(_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebug("LCG node provisioned: {@LcgNode}", node);
        
        return node;
    }
}