using Microsoft.AspNetCore.Mvc;
using OpenShock.Common.Models;
using OpenShock.Common.Redis;
using Redis.OM.Contracts;

namespace OpenShock.API.Controller.Public;

partial class PublicController
{
    [HttpGet("stats")]
    public async Task<BaseResponse<StatsResponse>> GetStats([FromServices] IRedisConnectionProvider redisConnectionProvider)
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