using OpenShock.Common.Models.WebSocket;
using OpenShock.Common.Models.WebSocket.LCG;
using OpenShock.LiveControlGateway.Controllers;
using OpenShock.ServicesCommon.Websocket;

namespace OpenShock.LiveControlGateway.Websocket;

/// <summary>
/// Websocket connection manager
/// </summary>
public static class WebsocketManager
{
    /// <summary>
    /// Live control users
    /// </summary>
    public static readonly SimpleWebsocketCollection<LiveControlController, IBaseResponse<LiveResponseType>> LiveControlUsers = new();
}