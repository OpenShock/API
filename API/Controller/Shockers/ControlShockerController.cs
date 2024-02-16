using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OpenShock.API.DeviceControl;
using OpenShock.Common.Models;
using OpenShock.ServicesCommon.DeviceControl;
using OpenShock.ServicesCommon.Hubs;
using OpenShock.ServicesCommon.Services.RedisPubSub;
using OpenShock.ServicesCommon.Utils;

namespace OpenShock.API.Controller.Shockers;

public sealed partial class ShockerController
{
    private static readonly IDictionary<string, object> EmptyDic = new Dictionary<string, object>();

    /// <summary>
    /// Send a control message to shockers
    /// </summary>
    /// <response code="200">The control messages were successfully sent.</response>
    [MapToApiVersion("2")]
    [HttpPost("control")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<BaseResponse<object>> SendControl(
        [FromBody] ControlRequest body,
        [FromServices] IHubContext<UserHub, IUserHub> userHub,
        [FromServices] IRedisPubService redisPubService)
    {
        var sender = new ControlLogSender
        {
            Id = CurrentUser.DbUser.Id,
            Name = CurrentUser.DbUser.Name,
            Image = GravatarUtils.GetImageUrl(CurrentUser.DbUser.Email),
            ConnectionId = HttpContext.Connection.Id,
            AdditionalItems = EmptyDic,
            CustomName = body.CustomName
        };

        await ControlLogic.ControlByUser(body.Shocks, _db, sender, userHub.Clients, redisPubService);

        return new BaseResponse<object>
        {
            Message = "Successfully sent control messages"
        };
    }

    /// <summary>
    /// Send a control message to shockers (Deprecated in favor of the /2/shockers/control endpoint)
    /// </summary>
    /// <response code="200">The control messages were successfully sent.</response>
    [MapToApiVersion("1")]
    [HttpPost("control")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<BaseResponse<object>> SendControl_DEPRECATED(
        [FromBody] IEnumerable<Common.Models.WebSocket.User.Control> body,
        [FromServices] IHubContext<UserHub, IUserHub> userHub,
        [FromServices] IRedisPubService redisPubService)
    {
        return SendControl(new ControlRequest
        {
            Shocks = body,
            CustomName = null
        }, userHub, redisPubService);
    }
}