using ShockLink.Common.Models.WebSocket;

namespace ShockLink.API.Realtime;

public static class WebsocketManager
{
    public static readonly WebsocketCollection<ResponseType> DeviceWebSockets = new();
}