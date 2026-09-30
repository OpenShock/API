using System.Text.Json;

using OpenShock.Internal.Common.JsonSerialization;

namespace OpenShock.Common.JsonSerialization;

public static class JsonOptions
{
    static JsonOptions()
    {
        ConfigureDefault(Default);
    }
    
    public static void ConfigureDefault(JsonSerializerOptions options)
    {
        options.PropertyNameCaseInsensitive = true;
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.Converters.Add(new PermissionTypeConverter());
        options.Converters.Add(new FlagGuardedJsonStringEnumConverter());
    }
    
    public static readonly JsonSerializerOptions Default = new();
}