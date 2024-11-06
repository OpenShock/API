namespace OpenShock.Common.JsonSerialization;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class TimeSpanToMillisecondsConverter : JsonConverter<TimeSpan>
{
    // Converts TimeSpan to JSON
    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        // Convert TimeSpan to total milliseconds and write it as a JSON number
        writer.WriteNumberValue(value.TotalMilliseconds);
    }

    // Converts JSON to TimeSpan
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException("Expected number representing milliseconds.");
        }
        
        // Read the milliseconds as a double and convert to TimeSpan
        var milliseconds = reader.GetDouble();
        return TimeSpan.FromMilliseconds(milliseconds);
    }
}
