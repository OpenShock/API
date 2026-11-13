// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace OpenShock.Common.Models.WebSocket;

public sealed class LiveControlResponse<T> where T : Enum
{
    public required T ResponseType { get; init; }
    public object? Data { get; init; }
    
}