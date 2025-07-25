﻿using Microsoft.AspNetCore.Mvc;
using NRedisStack.RedisStackCommands;
using OpenShock.Common.Models;
using OpenShock.Common.Redis;
using StackExchange.Redis;

namespace OpenShock.API.Controller.Public;

public sealed partial class PublicController
{
    /// <summary>
    /// Gets online devices statistics
    /// </summary>
    /// <response code="200">The statistics were successfully retrieved.</response>
    [HttpGet("stats")]
    [Tags("Meta")]
    public async Task<LegacyDataResponse<StatsResponse>> GetOnlineDevicesStatistics([FromServices] IConnectionMultiplexer redisConnectionMultiplexer)
    {
        var ft = redisConnectionMultiplexer.GetDatabase().FT();
        var deviceOnlineInfo = await ft.InfoAsync(DeviceOnline.IndexName);

        return new(new StatsResponse
        {
            DevicesOnline = deviceOnlineInfo.NumDocs
        });
    }
}

public sealed class StatsResponse
{
    public required long DevicesOnline { get; init; }
}