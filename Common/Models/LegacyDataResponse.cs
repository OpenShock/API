using System.Diagnostics.CodeAnalysis;

namespace OpenShock.Common.Models;

public sealed class LegacyDataResponse<T> where T : notnull
{
    [SetsRequiredMembers]
    public LegacyDataResponse(T data, string message = "")
    {
        Message = message;
        Data = data;
    }
    
    public required string Message { get; init; }
    public required T Data { get; init; }
}