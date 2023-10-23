using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenShock.Common.Models;
using OpenShock.Common.OpenShockDb;
using OpenShock.Common.Redis;
using OpenShock.Common.Serialization;
using OpenShock.ServicesCommon.Authentication;
using OpenShock.ServicesCommon.Utils;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using Semver;

namespace OpenShock.API.Controller.Admin.Monitoring;

[ApiController]
[Rank(RankType.Admin)]
[Route("/{version:apiVersion}/admin/monitoring/onlineDevices")]
public class OnlineDevicesController : AuthenticatedSessionControllerBase
{
    private readonly IRedisCollection<DeviceOnline> _devicesOnline;
    private readonly OpenShockContext _db;

    public OnlineDevicesController(IRedisConnectionProvider redisConnectionProvider, OpenShockContext db)
    {
        _db = db;
        _devicesOnline = redisConnectionProvider.RedisCollection<DeviceOnline>(false);
    }

    [HttpGet]
    public async Task<BaseResponse<object>> Get()
    {
        var allOnlineDevices = await _devicesOnline.ToListAsync();
        var dbLookup = await _db.Devices.Where(x => allOnlineDevices.Select(y => y.Id)
            .Contains(x.Id)).Select(
            x =>
                new
                {
                    Id = x.Id,
                    Name = x.Name,
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