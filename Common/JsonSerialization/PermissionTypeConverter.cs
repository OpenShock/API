using System.Text.Json;
using System.Text.Json.Serialization;
using OpenShock.Common.Models;

namespace OpenShock.Common.JsonSerialization;

public sealed class PermissionTypeConverter : JsonConverter<PermissionType>
{
    public override PermissionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    { 
        return PermissionTypeBindings.NameToPermissionType[reader.GetString()!];
    }

    public override void Write(Utf8JsonWriter writer, PermissionType value, JsonSerializerOptions options)
    {
        Console.WriteLine("dfgfdgfd");
        writer.WriteStringValue(PermissionTypeBindings.PermissionTypeToName[value]);
    }
}