using OpenShock.Common.Models.WebSocket;
using OpenShock.Common.Models.WebSocket.LCG;
using OpenShock.Common.Websocket;
using OpenShock.LiveControlGateway.Controllers;
using OpenShock.Serialization;

namespace OpenShock.LiveControlGateway.Websocket;

/// <summary>
/// Websocket connection manager
/// </summary>
public static class WebsocketManager
{
    /// <summary>
    /// Live control users
    /// </summary>
    public static readonly SimpleWebsocketCollection<LiveControlController, LiveControlResponse<LiveResponseType>> LiveControlUsers = new();
}