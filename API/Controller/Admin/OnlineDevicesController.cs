using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Models;
using OpenShock.Common.Redis;
using OpenShock.ServicesCommon.Utils;
using Semver;
using System.Net;
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
    [HttpGet("monitoring/onlineDevices", Name = "GetOnlineDevices")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<BaseResponse<object>> Get()
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

        return new BaseResponse<object>
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
                    Name = dbItem.Name
                };
            })
        };
    }

    public class AdminOnlineDeviceResponse
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required GenericIni Owner { get; set; }

        [JsonConverter(typeof(SemVersionJsonConverter))]
        public SemVersion? FirmwareVersion { get; set; }

        public string? Gateway { get; set; }
    }
}