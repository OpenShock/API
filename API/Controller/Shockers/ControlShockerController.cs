using Asp.Versioning;
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
[ApiVersion("1")]
[ApiVersion("2")]
[Route("/{version:apiVersion}/shockers")]
public class ControlShockerController : AuthenticatedSessionControllerBase
{
    private readonly ShockLinkContext _db;
    private readonly IHubContext<UserHub, IUserHub> _userHub;
    private readonly IDictionary<string, object> _emptyDic = new Dictionary<string, object>();

    public ControlShockerController(ShockLinkContext db, IHubContext<UserHub, IUserHub> userHub)
    {
        _db = db;
        _userHub = userHub;
    }

    [HttpPost("control")]
    [MapToApiVersion("2")]
    public async Task<BaseResponse<object>> ControlShocker(ControlRequest data)
    {
        var sender = new ControlLogSender
        {
            Id = CurrentUser.DbUser.Id,
            Name = CurrentUser.DbUser.Name,
            Image = ImagesApi.GetImageRoot(CurrentUser.DbUser.Image),
            ConnectionId = HttpContext.Connection.Id,
            AdditionalItems = _emptyDic,
            CustomName = data.CustomName
        };

        await ControlLogic.ControlByUser(data.Shocks, _db, sender, _userHub.Clients);

        return new BaseResponse<object>
        {
            Message = "Successfully sent control messages"
        };
    }

    [HttpPost("control")]
    [MapToApiVersion("1")]
    public Task<BaseResponse<object>> ControlShocker(IEnumerable<Common.Models.WebSocket.User.Control> data)
    {
        return ControlShocker(new ControlRequest
        {
            Shocks = data,
            CustomName = null
        });
    }
}