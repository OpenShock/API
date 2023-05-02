using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Redis.OM.Contracts;
using Redis.OM.Searching;
using ShockLink.API.Authentication;
using ShockLink.API.Models;
using ShockLink.API.Models.Requests;
using ShockLink.API.Utils;
using ShockLink.Common.Redis;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller.Devices;

[ApiController]
[Route("/{version:apiVersion}/devices")]
public class CreateController : AuthenticatedSessionControllerBase
{
    private readonly ShockLinkContext _db;
    private readonly IRedisCollection<DevicePair> _devicePairs;

    public CreateController(ShockLinkContext db, IRedisConnectionProvider provider)
    {
        _db = db;
        _devicePairs = provider.RedisCollection<DevicePair>();
    }

    [HttpGet]
    public async Task<BaseResponse<IEnumerable<Models.Response.Device>>> GetList()
    {
        var devices = await _db.Devices.Where(x => x.Owner == CurrentUser.DbUser.Id)
            .Select(x => new Models.Response.Device
            {
                Id = x.Id,
                Name = x.Name,
                CreatedOn = x.CreatedOn
            }).ToListAsync();
        return new BaseResponse<IEnumerable<Models.Response.Device>>
        {
           Data = devices
        };
    }

    [HttpGet("{id:guid}")]
    public async Task<BaseResponse<Models.Response.DeviceWithToken>> Get(Guid id)
    {
        var device = await _db.Devices.Where(x => x.Owner == CurrentUser.DbUser.Id && x.Id == id)
            .Select(x => new Models.Response.DeviceWithToken
            {
                Id = x.Id,
                Name = x.Name,
                CreatedOn = x.CreatedOn,
                Token = x.Token
            }).SingleOrDefaultAsync();
        if (device == null)
            return EBaseResponse<Models.Response.DeviceWithToken>("Device does not exist", HttpStatusCode.NotFound);
        return new BaseResponse<Models.Response.DeviceWithToken>
        {
            Data = device
        };
    }
    
    [HttpPatch("{id:guid}")]
    public async Task<BaseResponse<object>> Edit(Guid id, DeviceEdit data)
    {
        var device = await _db.Devices.Where(x => x.Owner == CurrentUser.DbUser.Id && x.Id == id).SingleOrDefaultAsync();
        if (device == null)
            return EBaseResponse<object>("Device does not exist", HttpStatusCode.NotFound);

        device.Name = data.Name;
        await _db.SaveChangesAsync();

        return new BaseResponse<object>
        {
            Message = "Successfully updated device"
        };
    }
        
    [HttpPut("{id:guid}")]
    public async Task<BaseResponse<object>> RegenToken(Guid id)
    {
        var device = await _db.Devices.Where(x => x.Owner == CurrentUser.DbUser.Id && x.Id == id).SingleOrDefaultAsync();
        if (device == null)
            return EBaseResponse<object>("Device does not exist", HttpStatusCode.NotFound);

        device.Token = CryptoUtils.RandomString(256);

        var affected = await _db.SaveChangesAsync();
        if (affected <= 0) return EBaseResponse<object>("Failed to save regenerated token", HttpStatusCode.InternalServerError);
        
        return new BaseResponse<object>
        {
            Message = "Successfully regenerated device token"
        };
    }
    
    [HttpDelete("{id:guid}")]
    public async Task<BaseResponse<object>> Delete(Guid id)
    {
        var affected = await _db.Devices.Where(x => x.Owner == CurrentUser.DbUser.Id && x.Id == id).ExecuteDeleteAsync();
        if (affected <= 0)
            return EBaseResponse<object>("Device does not exist", HttpStatusCode.NotFound);
        return new BaseResponse<object>
        {
            Message = "Successfully deleted device"
        };
    }

    [HttpPost]
    public async Task<BaseResponse<Guid>> CreateDevice()
    {
        var device = new Device
        {
            Id = Guid.NewGuid(),
            Owner = CurrentUser.DbUser.Id,
            Name = $"New Device {DateTimeOffset.UtcNow}",
            Token = CryptoUtils.RandomString(256)
        };
        _db.Devices.Add(device);
        await _db.SaveChangesAsync();
        
        Response.StatusCode = (int)HttpStatusCode.Created;
        return new BaseResponse<Guid>
        {
            Message = "Successfully created device",
            Data = device.Id
        };
    }

    [HttpGet("{id:guid}/pair")]
    public async Task<BaseResponse<string>> GetPairCode(Guid id)
    {
        var deviceExists = await _db.Devices.AnyAsync(x => x.Id == id && x.Owner == CurrentUser.DbUser.Id);
        if (!deviceExists)
            return EBaseResponse<string>("Device does not exists or does not belong to you", HttpStatusCode.NotFound);
        // replace with unlink?
        var existing = await _devicePairs.FindByIdAsync(id.ToString());
        if (existing != null) await _devicePairs.DeleteAsync(existing);
        
        var r = new Random();
        var pairCode = new DevicePair
        {
            Id = id,
            PairCode = r.Next(0, 1000000).ToString("000000")
        };
        await _devicePairs.InsertAsync(pairCode, TimeSpan.FromMinutes(10));

        return new BaseResponse<string>
        {
            Data = pairCode.PairCode
        };
    }
}