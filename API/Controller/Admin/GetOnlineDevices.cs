using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Extensions;
using OpenShock.Common.Models;
using OpenShock.Common.Redis;
using System.Text.Json.Serialization;
using OpenShock.Common.JsonSerialization;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Gets all online devices
    /// </summary>
    /// <response code="200">All online devices</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("monitoring/onlineDevices")]
    [ProducesResponseType<LegacyDataResponse<AdminOnlineDeviceResponse[]>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public async Task<IActionResult> GetOnlineDevices()
    {
        var devicesOnline = _redis.RedisCollection<DeviceOnline>(false);

        var allOnlineDevices = await devicesOnline.ToArrayAsync();
        var dbLookup = await _db.Devices
            .Where(x => allOnlineDevices.Select(y => y.Id).Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                x.Name,
                Owner = new BasicUserInfo
                {
                    Id = x.Owner.Id,
                    Image = x.Owner.GetImageUrl(),
                    Name = x.Owner.Name
                }
            })
            .ToArrayAsync();

        return LegacyDataOk(
            allOnlineDevices
                .Select(x =>
                {
                    var dbItem = dbLookup.First(y => y.Id == x.Id);
                    return new AdminOnlineDeviceResponse
                    {
                        Id = x.Id,
                        FirmwareVersion = x.FirmwareVersion,
                        Gateway = x.Gateway,
                        Owner = dbItem.Owner,
                        Name = dbItem.Name,
                        ConnectedAt = x.ConnectedAt,
                        UserAgent = x.UserAgent,
                        BootedAt = x.BootedAt,
                        LatencyMs = x.LatencyMs,
                        Rssi = x.Rssi,
                    };
                })
        );
    }

    public sealed class AdminOnlineDeviceResponse
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public required BasicUserInfo Owner { get; init; }

        [JsonConverter(typeof(SemVersionJsonConverter))]
        public required SemVersion FirmwareVersion { get; init; }

        public required string Gateway { get; init; }
        public required DateTimeOffset ConnectedAt { get; init; }
        
        public required string? UserAgent { get; init; }
        public required DateTimeOffset BootedAt { get; init; }
        public required ushort? LatencyMs { get; init; }
        public required int? Rssi { get; init; }
    }
}