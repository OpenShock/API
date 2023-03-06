using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShockLink.API.Authentication;
using ShockLink.API.Models;
using ShockLink.API.Utils;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller.Devices;

[ApiController]
[Route("/{version:apiVersion}/devices")]
public class CreateController : AuthenticatedSessionControllerBase
{
    private readonly ShockLinkContext _db;
    
    public CreateController(ShockLinkContext db)
    {
        _db = db;
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
}