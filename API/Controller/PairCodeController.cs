using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Redis.OM;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using ShockLink.API.Models;
using ShockLink.Common.Redis;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller;

[ApiController]
[Route("/{version:apiVersion}/pair")]
public class PairCodeController : ShockLinkControllerBase
{
    private readonly IRedisCollection<DevicePair> _devicePairs;
    private readonly ShockLinkContext _shockLinkContext;

    public PairCodeController(IRedisConnectionProvider provider, ShockLinkContext shockLinkContext)
    {
        _shockLinkContext = shockLinkContext;
        _devicePairs = provider.RedisCollection<DevicePair>();
    }

    [HttpGet("{pairCode}")]
    public async Task<BaseResponse<string>> Get(string pairCode)
    {
        var pair = await _devicePairs.Where(x => x.PairCode == pairCode).SingleOrDefaultAsync();
        if (pair == null) return EBaseResponse<string>("No such pair code exists", HttpStatusCode.NotFound);
        await _devicePairs.DeleteAsync(pair);
        
        var device = await _shockLinkContext.Devices.SingleOrDefaultAsync(x => x.Id == pair.Id);
        if (device == null) return EBaseResponse<string>("No such device exists for the pair code", HttpStatusCode.InternalServerError);

        return new BaseResponse<string>
        {
            Data = device.Token
        };
    }
}