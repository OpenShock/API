using Microsoft.AspNetCore.SignalR;
using OpenShock.Common.Hubs;
using OpenShock.Common.Models;
using OpenShock.Common.Models.WebSocket.User;
using OpenShock.Common.Results;

namespace OpenShock.Common.Services;

public interface IControlSender
{
    public Task<ShockerControlResult> ControlByUser(IReadOnlyList<Control> controls, ControlLogSender sender, IHubClients<IUserHub> hubClients, ApiTokenControlLimits? tokenLimits = null);

    public Task<ShockerControlResult> ControlPublicShare(IReadOnlyList<Control> controls, ControlLogSender sender, IHubClients<IUserHub> hubClients, Guid publicShareId);
}

/// <summary>
/// Result of a shocker control attempt. The error cases are reference types, so storing them in
/// the union's internal <c>object?</c> slot is a plain reference assignment, not a boxing conversion.
/// </summary>
public union ShockerControlResult(Success, NotFound<Guid>, ShockerPaused, ShockerNoPermission);

public sealed record ShockerPaused(Guid Value);

public sealed record ShockerNoPermission(Guid Value);