using Microsoft.AspNetCore.SignalR;
using OneOf;
using OneOf.Types;
using OpenShock.Common.Hubs;
using OpenShock.Common.Models;
using OpenShock.Common.Models.WebSocket.User;

namespace OpenShock.Common.Services;

public interface IControlSender
{
    public Task<OneOf<Success, ShockerNotFoundOrNoAccess, ShockerPaused, ShockerNoPermission>> ControlByUser(IReadOnlyList<Control> controls, ControlLogSender sender, IHubClients<IUserHub> hubClients);

    public Task<OneOf<Success, ShockerNotFoundOrNoAccess, ShockerPaused, ShockerNoPermission>> ControlPublicShare(IReadOnlyList<Control> controls, ControlLogSender sender, IHubClients<IUserHub> hubClients, Guid publicShareId);
}

public readonly record struct ShockerNotFoundOrNoAccess(Guid Value);

public readonly record struct ShockerPaused(Guid Value);

public readonly record struct ShockerNoPermission(Guid Value);