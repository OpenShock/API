using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Redis;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Gets all online gateways
    /// </summary>
    /// <response code="200">All online gateways</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("monitoring/gateways")]
    [ProducesResponseType<AdminOnlineGatewayResponse[]>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public async Task<AdminOnlineGatewayResponse[]> GetOnlineGateways()
    {
        PedanticallyEnsureAdmin();

        var gateways = await _redis.RedisCollection<LcgNode>(false).ToListAsync();
        var hubsPerGateway = (await _redis.RedisCollection<DeviceOnline>(false).ToListAsync())
            .CountBy(x => x.Gateway)
            .ToDictionary();

        return gateways
            .OrderBy(x => x.Environment)
            .ThenBy(x => x.Id)
            .Select(x => new AdminOnlineGatewayResponse
            {
                Id = x.Id,
                Host = x.Host,
                Port = x.Port,
                PathPrefix = x.PathPrefix,
                Country = x.Country,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                Load = x.Load,
                Environment = x.Environment,
                ConnectedHubs = hubsPerGateway.GetValueOrDefault(x.Id)
            })
            .ToArray();
    }

    public sealed class AdminOnlineGatewayResponse
    {
        public required string Id { get; init; }
        public required string Host { get; init; }
        public required ushort Port { get; init; }
        public required string PathPrefix { get; init; }
        public required string Country { get; init; }
        public required double? Latitude { get; init; }
        public required double? Longitude { get; init; }

        /// <summary>
        /// Load the gateway advertises for hub assignment, in percent
        /// </summary>
        public required byte Load { get; init; }

        public required string Environment { get; init; }
        public required int ConnectedHubs { get; init; }
    }
}
