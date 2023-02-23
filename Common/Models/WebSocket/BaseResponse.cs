// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace ShockLink.Common.Models.WebSocket;

public class BaseResponse : IBaseResponse<ResponseType>
{
    public required ResponseType ResponseType { get; set; }
    public object? Data { get; set; }
    
}