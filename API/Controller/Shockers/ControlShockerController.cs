using System.Net;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OpenShock.Common.Models;
using OpenShock.Common.Utils;
using OpenShock.ServicesCommon.Authentication.Attributes;
using OpenShock.ServicesCommon.DeviceControl;
using OpenShock.ServicesCommon.Errors;
using OpenShock.ServicesCommon.Hubs;
using OpenShock.ServicesCommon.Problems;
using OpenShock.ServicesCommon.Services.RedisPubSub;

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
    [TokenPermission(PermissionType.Shockers_Use)]
    [ProducesSuccess]
    [ProducesProblem(HttpStatusCode.NotFound, "Shocker not found")]
    [ProducesProblem(HttpStatusCode.PreconditionFailed, "Shocker is paused")]
    [ProducesProblem(HttpStatusCode.Forbidden, "You don't have permission to control this shocker")]
    public async Task<IActionResult> SendControl(
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

        var controlAction = await ControlLogic.ControlByUser(body.Shocks, _db, sender, userHub.Clients, redisPubService);
        return controlAction.Match(
            success => RespondSuccessSimple("Successfully sent control messages"),
            notFound => Problem(ShockerControlError.ShockerControlNotFound(notFound.Value)),
            paused => Problem(ShockerControlError.ShockerControlPaused(paused.Value)),
            noPermission => Problem(ShockerControlError.ShockerControlNoPermission(noPermission.Value)));
    }

    /// <summary>
    /// Send a control message to shockers (Deprecated in favor of the /2/shockers/control endpoint)
    /// </summary>
    /// <response code="200">The control messages were successfully sent.</response>
    [MapToApiVersion("1")]
    [HttpPost("control")]
    [TokenPermission(PermissionType.Shockers_Use)]
    [ProducesSuccess]
    [ProducesProblem(HttpStatusCode.NotFound, "Shocker not found")]
    [ProducesProblem(HttpStatusCode.PreconditionFailed, "Shocker is paused")]
    [ProducesProblem(HttpStatusCode.Forbidden, "You don't have permission to control this shocker")]
    public Task<IActionResult> SendControl_DEPRECATED(
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