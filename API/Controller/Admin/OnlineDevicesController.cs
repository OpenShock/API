using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Models;
using OpenShock.Common.Redis;
using OpenShock.ServicesCommon.Utils;
using Semver;
using System.Text.Json.Serialization;
using OpenShock.Common.JsonSerialization;
using OpenShock.ServicesCommon.Problems;

namespace OpenShock.API.Controller.Admin;

public sealed partial class AdminController
{
    /// <summary>
    /// Gets all online devices
    /// </summary>
    /// <response code="200">All online devices</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("monitoring/onlineDevices")]
    [ProducesSuccess<IEnumerable<AdminOnlineDeviceResponse>>]
    public async Task<BaseResponse<IEnumerable<AdminOnlineDeviceResponse>>> GetOnlineDevices()
    {
        var devicesOnline = _redis.RedisCollection<DeviceOnline>(false);

        var allOnlineDevices = await devicesOnline.ToListAsync();
        var dbLookup = await _db.Devices.Where(x => allOnlineDevices.Select(y => y.Id)
            .Contains(x.Id)).Select(
            x =>
                new
                {
                    x.Id,
                    x.Name,
                    Owner = new GenericIni
                    {
                        Id = x.OwnerNavigation.Id,
                        Image = x.OwnerNavigation.GetImage(),
                        Name = x.OwnerNavigation.Name
                    }
                }).ToListAsync();

        return new BaseResponse<IEnumerable<AdminOnlineDeviceResponse>>
        {
            Data = allOnlineDevices.Select(x =>
            {
                var dbItem = dbLookup.First(y => y.Id == x.Id);
                return new AdminOnlineDeviceResponse
                {
                    Id = x.Id,
                    FirmwareVersion = x.FirmwareVersion,
                    Gateway = x.Gateway,
                    Owner = dbItem.Owner,
                    Name = dbItem.Name,
                    ConnectedAt = x.ConnectedAt
                };
            })
        };
    }

    public sealed class AdminOnlineDeviceResponse
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public required GenericIni Owner { get; init; }

        [JsonConverter(typeof(SemVersionJsonConverter))]
        public required SemVersion? FirmwareVersion { get; init; }

        public required string? Gateway { get; init; }
        public required DateTimeOffset ConnectedAt { get; init; }
    }
}