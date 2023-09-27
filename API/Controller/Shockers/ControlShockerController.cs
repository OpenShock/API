using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OpenShock.API.DeviceControl;
using OpenShock.API.Hubs;
using OpenShock.Common.Models;
using OpenShock.ServicesCommon.Utils;

namespace OpenShock.API.Controller.Shockers;

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
            Image = GravatarUtils.GetImageUrl(CurrentUser.DbUser.Email),
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