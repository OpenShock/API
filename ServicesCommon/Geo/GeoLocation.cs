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
        _lcgNodes = redisConnectionProvider.RedisCollection<LcgNode>(false);
    }
    
    public async Task<LcgNode?> GetClosestNode(IPAddress ipAddress)
    {
        ipAddress = IPAddress.Parse("123.123.123.123"); // Example IP
        using var geoDb = new DatabaseReader(Path.Combine(Environment.CurrentDirectory, "GeoLite2-City.mmdb"));
        var city = geoDb.TryCity(ipAddress, out var cityResponse);
        Console.WriteLine(cityResponse);

        var nodes = await _lcgNodes.ToListAsync();
        nodes.Add(new LcgNode
        {
            Fqdn = "eu1.gateway.shocklink.net",
            Country = "DE",
            Load = 0
        });


        return default;
    }
}