// ReSharper disable UnusedAutoPropertyAccessor.Global

using System.Text.Json;

namespace OpenShock.Common.Models;

public sealed class LegacyEmptyResponse
{
    public string? Message { get; set; }
    public object? Data { get; set; } = null;

    public LegacyEmptyResponse(string? message = null)
    {
        Message = message;
    }
    
    public override string ToString() => JsonSerializer.Serialize(this);
}