// ReSharper disable UnusedAutoPropertyAccessor.Global

using System.Text.Json;

namespace OpenShock.Common.Models;

public sealed class BaseResponse<T>
{
    public string? Message { get; set; }
    public T? Data { get; set; }

    public BaseResponse(string? message = null, T? data = default)
    {
        Message = message;
        Data = data;
    }
    
    public override string ToString() => JsonSerializer.Serialize(this);
}