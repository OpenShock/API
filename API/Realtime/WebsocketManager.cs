
namespace OpenShock.API.Realtime;

public static class WebsocketManager
{
    public static readonly WebsocketCollection<Common.Models.WebSocket.Device.ResponseType> DeviceWebSockets = new();
}