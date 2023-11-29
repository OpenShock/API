using OpenShock.Common.Models.WebSocket;
using OpenShock.Common.Models.WebSocket.LCG;
using OpenShock.Serialization;
using OpenShock.ServicesCommon.Websocket;

namespace OpenShock.LiveControlGateway.Websocket;

public static class WebsocketManager
{
    public static readonly WebsocketCollection<ServerToDeviceMessage> ServerToDevice = new();
    
    public static readonly WebsocketCollection<IBaseResponse<LiveResponseType>> LiveControlUsers = new();
}