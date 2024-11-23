namespace OpenShock.Common.JsonSerialization;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class UnixMillisecondsDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    // Serialize DateTimeOffset to Unix time in milliseconds
    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        var unixTimeMilliseconds = value.ToUnixTimeMilliseconds();
        writer.WriteNumberValue(unixTimeMilliseconds);
    }

    // Deserialize Unix time in milliseconds to DateTimeOffset
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException("Expected number token for Unix time in milliseconds.");
        }

        var unixTimeMilliseconds = reader.GetInt64();
        return DateTimeOffset.FromUnixTimeMilliseconds(unixTimeMilliseconds);
    }
}
