using System.Net.Mime;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OpenShock.Common.Authentication.Attributes;
using OpenShock.Common.DeviceControl;
using OpenShock.Common.Errors;
using OpenShock.Common.Extensions;
using OpenShock.Common.Hubs;
using OpenShock.Common.Models;
using OpenShock.Common.Problems;

namespace OpenShock.API.Controller.Shockers;

public sealed partial class ShockerController
{
    /// <summary>
    /// Send a control message to shockers
    /// </summary>
    /// <response code="200">The control messages were successfully sent.</response>
    [MapToApiVersion("2")]
    [HttpPost("control")]
    [TokenPermission(PermissionType.Shockers_Use)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<LegacyEmptyResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // Shocker not found
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status412PreconditionFailed, MediaTypeNames.Application.ProblemJson)] // Shocker is paused
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)] // You don't have permission to control this shocker
    public async Task<IActionResult> SendControl(
        [FromBody] ControlRequest body,
        [FromServices] IHubContext<UserHub, IUserHub> userHub,
        [FromServices] IControlSender controlSender)
    {
        var sender = new ControlLogSender
        {
            Id = CurrentUser.Id,
            Name = CurrentUser.Name,
            Image = CurrentUser.GetImageUrl(),
            ConnectionId = HttpContext.Connection.Id,
            AdditionalItems = [],
            CustomName = body.CustomName
        };

        var controlAction = await controlSender.ControlByUser(body.Shocks, sender, userHub.Clients);
        return controlAction.Match(
            success => LegacyEmptyOk("Successfully sent control messages"),
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
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<LegacyEmptyResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)] // Shocker not found
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status412PreconditionFailed, MediaTypeNames.Application.ProblemJson)] // Shocker is paused
    [ProducesResponseType<OpenShockProblem>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)] // You don't have permission to control this shocker
    public Task<IActionResult> SendControl_DEPRECATED(
        [FromBody] IReadOnlyList<Common.Models.WebSocket.User.Control> body,
        [FromServices] IHubContext<UserHub, IUserHub> userHub,
        [FromServices] IControlSender controlSender)
    {
        return SendControl(new ControlRequest
        {
            Shocks = body,
            CustomName = null
        }, userHub, controlSender);
    }
}