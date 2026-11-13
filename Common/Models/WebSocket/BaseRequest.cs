using System.Text.Json;

namespace OpenShock.Common.Models.WebSocket;

public sealed class BaseRequest<T>
{
    public required T RequestType { get; init; }
    public JsonDocument? Data { get; init; }
}