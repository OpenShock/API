using OpenShock.Serialization;

namespace OpenShock.LiveControlGateway.Websocket;

public static class WebsocketManager
{
    public static readonly WebsocketCollection<ServerToDeviceMessage> ServerToDevice = new();
}