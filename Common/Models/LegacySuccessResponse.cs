// ReSharper disable UnusedAutoPropertyAccessor.Global

using System.Text.Json;

namespace OpenShock.Common.Models;

public sealed class LegacySuccessResponse<T>
{
    public string Message { get; set; }
    public T Data { get; set; }

    public LegacySuccessResponse(T data, string message = "")
    {
        Message = message;
        Data = data;
    }
    
    public override string ToString() => JsonSerializer.Serialize(this);
}