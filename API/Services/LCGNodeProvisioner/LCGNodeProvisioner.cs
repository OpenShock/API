using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Constants;
using OpenShock.Internal.Common.Geo;
using OpenShock.Common.Redis;
using OpenShock.Common.Services.Geo;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Searching;

using OpenShock.Internal.Common.Constants;

namespace OpenShock.API.Services.LCGNodeProvisioner;

public sealed class LCGNodeProvisioner : ILCGNodeProvisioner
{
    /// <summary>
    /// Slack within which a node still counts as closest when narrowing by coordinates, so nearby nodes
    /// keep being load balanced. Coordinate distances almost never tie exactly, and GeoIP city locations
    /// are only good to tens of kilometres anyway, so without slack one node per metro would take every hub.
    /// </summary>
    private const double CoordinateToleranceKm = 150;

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

    public Task<LcgNode?> GetOptimalNodeAsync(Alpha2CountryCode countryCode) => GetOptimalNodeAsync(countryCode, null);

    public async Task<LcgNode?> GetOptimalNodeAsync(Alpha2CountryCode countryCode, GeoPoint? clientLocation)
    {
        if (countryCode.IsUnknown() && clientLocation is null)
        {
            _logger.LogInformation("Country code is unknown, getting optimal node without geo location information");
            return await GetOptimalNodeAsync();
        }

        // Load all nodes for our environment
        var nodes = await _lcgNodes
            .Where(x => x.Environment == _environmentName)
            .ToListAsync();
        
        if(nodes.Count < 1)
        {
            _logger.LogWarning("No LCG nodes available after filtering by environment [{Environment}]!", _environmentName);
            return null;
        }
        
        // Precompute distances
        var withDistances = nodes
            .Select(x => new
            {
                Node = x,
                Distance = !countryCode.IsUnknown() && DistanceLookup.TryGetDistanceBetween(x.Country, countryCode, out var dist) ? dist : Distance.DistanceToAndromedaGalaxyInKm
            })
            .ToArray();
        
        // 1) Find the closest region (min distance)
        var minDistance = withDistances.Min(x => x.Distance);

        var closestRegionNodes = withDistances
            .Where(x => Math.Abs(x.Distance - minDistance) < 1)
            .Select(x => x.Node)
            .ToArray();

        // 2) Narrow that region by coordinates. This only ever refines the country pick, never overrides
        //    it, because coordinate and country-table distances don't compare: a node without
        //    coordinates would score 0 km for a same-country client and beat every node that has them.
        if (clientLocation is { } client)
        {
            closestRegionNodes = NarrowByCoordinates(closestRegionNodes, client);
        }

        // 3) Among those, find minimal load
        var minLoad = closestRegionNodes.Min(x => x.Load);
        var loadCandidates = closestRegionNodes
            .Where(x => x.Load == minLoad)
            .ToArray();
        
        if(loadCandidates.Length < 1)
        {
            _logger.LogWarning("No LCG nodes available after filtering by geo location and load!");
            return null;
        }

        // 4) Randomly pick one of the tied nodes
        var node = loadCandidates[Random.Shared.Next(loadCandidates.Length)];
        
        if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebug("LCG node provisioned: {@LcgNode}", node);

        return node;
    }

    /// <summary>
    /// Keeps the nodes within <see cref="CoordinateToleranceKm"/> of the nearest one. All or nothing: if
    /// any node in the region lacks coordinates there is no fair way to rank it, so the region is
    /// returned untouched and assignment behaves exactly as it did before coordinates existed.
    /// </summary>
    private static LcgNode[] NarrowByCoordinates(LcgNode[] nodes, GeoPoint client)
    {
        var withDistances = new (LcgNode Node, double Distance)[nodes.Length];
        for (var i = 0; i < nodes.Length; i++)
        {
            if (GeoPoint.From(nodes[i].Latitude, nodes[i].Longitude) is not { } location) return nodes;
            withDistances[i] = (nodes[i], client.DistanceToKm(location));
        }

        var minDistance = withDistances.Min(x => x.Distance);

        return withDistances
            .Where(x => x.Distance - minDistance <= CoordinateToleranceKm)
            .Select(x => x.Node)
            .ToArray();
    }
}