using System.Text.Json;

namespace ShockLink.API.Serialization;

public static class SlSerializer
{
    private static readonly JsonSerializerOptions? DefaultSerializerSettings = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    public static T? Deserialize<T>(this string json)
    {       
        return JsonSerializer.Deserialize<T>(json, DefaultSerializerSettings);
    }
    
    public static TValue? SlDeserialize<TValue>(this JsonDocument? document)
    {
        return document is null ? default : document.Deserialize<TValue>(DefaultSerializerSettings);
    }
}