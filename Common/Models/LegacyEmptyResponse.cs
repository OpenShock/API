using System.Diagnostics.CodeAnalysis;

namespace OpenShock.Common.Models;

public sealed class LegacyEmptyResponse
{
    [SetsRequiredMembers]
    public LegacyEmptyResponse(string message, object? data = null)
    {
        Message = message;
        Data = data;
    }
    
    public required string Message { get; set; }
    public object? Data { get; set; }
}