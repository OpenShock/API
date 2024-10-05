
using OpenShock.Common.Models.WebSocket;
using OpenShock.Common.Models.WebSocket.Device;
using OpenShock.Common.Websocket;

namespace OpenShock.API.Realtime;

public static class WebsocketManager
{
    public static readonly WebsocketCollection<IBaseResponse<ResponseType>> DeviceWebSockets = new();
}