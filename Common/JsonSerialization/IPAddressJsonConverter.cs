using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenShock.Common.JsonSerialization;

/// <summary>
/// Writes <see cref="IPAddress"/> as its textual form instead of letting the reflection-based
/// serializer expose the .NET type's members (addressFamily, scopeId, ...).
/// </summary>
public sealed class IPAddressJsonConverter : JsonConverter<IPAddress>
{
    public override IPAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("IP address must be a string");
        }

        if (!IPAddress.TryParse(reader.GetString(), out var address))
        {
            throw new JsonException("String is not a valid IP address");
        }

        return address;
    }

    public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
}
