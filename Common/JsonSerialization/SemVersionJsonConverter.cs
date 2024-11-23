using System.Text.Json;
using System.Text.Json.Serialization;
using Semver;

namespace OpenShock.Common.JsonSerialization;

public sealed class SemVersionJsonConverter : JsonConverter<SemVersion>
{
    public override SemVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            throw new JsonException("SemVer cannot be null");
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("SemVer must be a string");
        }

        string? str = reader.GetString();
        if (string.IsNullOrEmpty(str))
        {
            throw new JsonException("SemVer cannot be empty");
        }

        if (!SemVersion.TryParse(str, SemVersionStyles.Strict, out SemVersion? version))
        {
            throw new JsonException("String is not a valid SemVer");
        }
        
        return version;
    }
    
    public override void Write(Utf8JsonWriter writer, SemVersion value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
}