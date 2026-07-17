using Microsoft.AspNetCore.SignalR;
using OpenShock.Common.Hubs;
using OpenShock.Common.Models;
using OpenShock.Common.Models.WebSocket.User;
using OpenShock.Common.Results;

namespace OpenShock.Common.Services;

public interface IControlSender
{
    public Task<Union4<Success, ShockerNotFoundOrNoAccess, ShockerPaused, ShockerNoPermission>> ControlByUser(IReadOnlyList<Control> controls, ControlLogSender sender, IHubClients<IUserHub> hubClients, ApiTokenControlLimits? tokenLimits = null);

    public Task<Union4<Success, ShockerNotFoundOrNoAccess, ShockerPaused, ShockerNoPermission>> ControlPublicShare(IReadOnlyList<Control> controls, ControlLogSender sender, IHubClients<IUserHub> hubClients, Guid publicShareId);
}

public readonly record struct ShockerNotFoundOrNoAccess(Guid Value);

public readonly record struct ShockerPaused(Guid Value);

public readonly record struct ShockerNoPermission(Guid Value);