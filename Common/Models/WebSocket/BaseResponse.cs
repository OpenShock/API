// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace OpenShock.Common.Models.WebSocket;

public class BaseResponse<T> : IBaseResponse<T> where T : Enum
{
    public required T ResponseType { get; set; }
    public object? Data { get; set; }
    
}