using OpenShock.Common.Models.WebSocket;
using OpenShock.Common.Models.WebSocket.LCG;
using OpenShock.LiveControlGateway.Controllers;
using OpenShock.Serialization;
using OpenShock.ServicesCommon.Websocket;

namespace OpenShock.LiveControlGateway.Websocket;

public static class WebsocketManager
{
    public static readonly SimpleWebsocketCollection<DeviceController, ServerToDeviceMessage> ServerToDevice = new();
    
    public static readonly SimpleWebsocketCollection<LiveControlController, IBaseResponse<LiveResponseType>> LiveControlUsers = new();
}