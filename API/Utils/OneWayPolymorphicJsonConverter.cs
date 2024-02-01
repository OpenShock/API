using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenShock.API.Utils;

public class OneWayPolymorphicJsonConverter<G> : JsonConverter<G>
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(G) == typeToConvert; //.IsAssignableFrom(typeToConvert);
    }
    
    public override G Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException();

    public override void Write(Utf8JsonWriter writer, G value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value, value.GetType());
}