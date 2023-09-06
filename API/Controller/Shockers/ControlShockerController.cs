using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ShockLink.API.DeviceControl;
using ShockLink.API.Hubs;
using ShockLink.API.Models;
using ShockLink.API.Utils;
using ShockLink.Common.Models;

namespace ShockLink.API.Controller.Shockers;

public sealed partial class ShockerController
{
    private static readonly IDictionary<string, object> EmptyDic = new Dictionary<string, object>();

    [HttpPost("control")]
    [MapToApiVersion("2")]
    public async Task<BaseResponse<object>> ControlShockerV2(ControlRequest data,
        [FromServices] IHubContext<UserHub, IUserHub> userHub)
    {
        var sender = new ControlLogSender
        {
            Id = CurrentUser.DbUser.Id,
            Name = CurrentUser.DbUser.Name,
            Image = ImagesApi.GetImageRoot(CurrentUser.DbUser.Image),
            ConnectionId = HttpContext.Connection.Id,
            AdditionalItems = EmptyDic,
            CustomName = data.CustomName
        };

        await ControlLogic.ControlByUser(data.Shocks, _db, sender, userHub.Clients);

        return new BaseResponse<object>
        {
            Message = "Successfully sent control messages"
        };
    }

    [HttpPost("control")]
    [MapToApiVersion("1")]
    public Task<BaseResponse<object>> ControlShocker(IEnumerable<Common.Models.WebSocket.User.Control> data,
        [FromServices] IHubContext<UserHub, IUserHub> userHub)
    {
        return ControlShockerV2(new ControlRequest
        {
            Shocks = data,
            CustomName = null
        }, userHub);
    }
}