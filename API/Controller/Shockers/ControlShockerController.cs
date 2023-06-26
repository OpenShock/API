using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ShockLink.API.Authentication;
using ShockLink.API.DeviceControl;
using ShockLink.API.Hubs;
using ShockLink.API.Models;
using ShockLink.API.Utils;
using ShockLink.Common.Models;
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
    public async Task<BaseResponse<object>> ControlShocker(IEnumerable<Common.Models.WebSocket.User.Control> data)
    {
        var sender = new ControlLogSender
        {
            Id = CurrentUser.DbUser.Id,
            Name = CurrentUser.DbUser.Name,
            Image = ImagesApi.GetImageRoot(CurrentUser.DbUser.Image),
            ConnectionId = HttpContext.Connection.Id,
            AdditionalItems = new Dictionary<string, object>()
        };

        await ControlLogic.Control(data, _db, sender, _userHub.Clients);

        return new BaseResponse<object>
        {
            Message = "Successfully sent control messages"
        };
    }
}