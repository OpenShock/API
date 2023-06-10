using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ShockLink.API.Authentication;
using ShockLink.API.DeviceControl;
using ShockLink.API.Hubs;
using ShockLink.API.Models;
using ShockLink.API.Models.Requests;
using ShockLink.Common.ShockLinkDb;

namespace ShockLink.API.Controller.Shockers;

[ApiController]
[Route("/{version:apiVersion}/shockers")]
public class ControlShockerController : AuthenticatedSessionControllerBase
{
    private readonly ShockLinkContext _db;
    private readonly IHubContext<UserHub, IUserHub> _userHub;

    public ControlShockerController(ShockLinkContext db, IHubContext<UserHub, IUserHub> userHub)
    {
        _db = db;
        _userHub = userHub;
    }
    
    [HttpPost("control")]
    public async Task<BaseResponse<object>> EditShocker(IEnumerable<Common.Models.WebSocket.User.Control> data)
    {
        await ControlLogic.Control(data, _db, CurrentUser.DbUser.Id, _userHub.Clients);
        
        return new BaseResponse<object>
        {
            Message = "Successfully sent control messages"
        };
    }
}