using System.Text.Json;

namespace OpenShock.Common.JsonSerialization;

public static class JsonSettings
{
    public static readonly JsonSerializerOptions FlagCompatibleCaseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new FlagCompatibleJsonStringEnumConverter() }
    };
    
    public static readonly JsonSerializerOptions CaseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    public static readonly JsonSerializerOptions CamelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}