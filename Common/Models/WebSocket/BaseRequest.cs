using System.Text.Json;

namespace OpenShock.Common.Models.WebSocket;

public class BaseRequest<T>
{
    public required T RequestType { get; set; }
    public JsonDocument? Data { get; set; }
}