using System.Net;
using MaxMind.GeoIP2;
using OpenShock.Common.Redis;
using Redis.OM.Contracts;
using Redis.OM.Searching;

namespace OpenShock.ServicesCommon.Geo;

public class GeoLocation : IGeoLocation
{
    private readonly IRedisCollection<LcgNode> _lcgNodes;
    
    public GeoLocation(IRedisConnectionProvider redisConnectionProvider)
    {
        _lcgNodes = redisConnectionProvider.RedisCollection<LcgNode>();
    }
    
    public async Task GetClosestNode(IPAddress ipAddress)
    {
        using var geoDb = new DatabaseReader(Path.Combine(Environment.CurrentDirectory, "GeoLite2-City.mmdb"));
        var city = geoDb.TryCity(ipAddress, out var cityResponse);

        var nodes = await _lcgNodes.ToListAsync();
        
    }
}