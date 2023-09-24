using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using ShockLink.API.Models;
using ShockLink.Common.Redis;

namespace ShockLink.API.Controller.Public;

[ApiController]
[Route("/{version:apiVersion}/public/stats")]
[AllowAnonymous]
public class StatsController : ShockLinkControllerBase
{
    private readonly IRedisCollection<DeviceOnline> _deviceOnlines;

    public StatsController(IRedisConnectionProvider redis)
    {
        _deviceOnlines = redis.RedisCollection<DeviceOnline>();
    }

    [HttpGet]
    public async Task<BaseResponse<StatsResponse>> Get()
    {
        return new BaseResponse<StatsResponse>
        {
            Data = new StatsResponse
            {
                DevicesOnline = await _deviceOnlines.CountAsync()
            }
        };
    }
}

public class StatsResponse
{
    public required int DevicesOnline { get; set; }
}