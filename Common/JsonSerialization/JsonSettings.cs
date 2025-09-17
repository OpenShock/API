using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenShock.Common.JsonSerialization;

public static class JsonSettings
{
    private static void ConfigureBase(JsonSerializerOptions options)
    {
        options.PropertyNameCaseInsensitive = true;
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.Converters.Add(new PermissionTypeConverter());
    }
    
    public static void HttpOptions(Microsoft.AspNetCore.Http.Json.JsonOptions options)
    {
        ConfigureBase(options.SerializerOptions);
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()); // TODO: Why does this differ from the one below?
    }
    
    public static void MvcOptions(Microsoft.AspNetCore.Mvc.JsonOptions options)
    {
        ConfigureBase(options.JsonSerializerOptions);
        options.JsonSerializerOptions.Converters.Add(new FlagCompatibleJsonStringEnumConverter()); // TODO: Why does this differ from the one above?
    }
    
    public static readonly JsonSerializerOptions MailJetSettings = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    public static readonly JsonSerializerOptions LiveControlSettings = new() // TODO: Why does this differ from the one below?
    {
        PropertyNameCaseInsensitive = true
    };
    public static readonly JsonSerializerOptions LiveControlSettings2 = new() // TODO: Why does this differ from the one above?
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new FlagCompatibleJsonStringEnumConverter() }
    };
}