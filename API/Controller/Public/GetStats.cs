using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Models;
using OpenShock.Common.Redis;
using OpenShock.ServicesCommon.Problems;
using Redis.OM.Contracts;

namespace OpenShock.API.Controller.Public;

public sealed partial class PublicController
{
    /// <summary>
    /// Gets online devices statistics
    /// </summary>
    /// <response code="200">The statistics were successfully retrieved.</response>
    [HttpGet("stats")]
    [ProducesSuccess<StatsResponse>]
    public async Task<BaseResponse<StatsResponse>> GetOnlineDevicesStatistics(
        [FromServices] IRedisConnectionProvider redisConnectionProvider)
    {
        var deviceOnlines = redisConnectionProvider.RedisCollection<DeviceOnline>(false);

        return new BaseResponse<StatsResponse>
        {
            Data = new StatsResponse
            {
                DevicesOnline = await deviceOnlines.CountAsync()
            }
        };
    }
}

public class StatsResponse
{
    public required int DevicesOnline { get; set; }
}