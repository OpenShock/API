
namespace ShockLink.API.Realtime;

public static class WebsocketManager
{
    public static readonly WebsocketCollection<Common.Models.WebSocket.Device.ResponseType> DeviceWebSockets = new();
    public static readonly WebsocketCollection<Common.Models.WebSocket.User.ResponseType> UserWebSockets = new();
}