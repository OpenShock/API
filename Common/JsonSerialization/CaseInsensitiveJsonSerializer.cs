using System.Text.Json;

namespace OpenShock.Common.JsonSerialization;

public static class CaseInsensitiveJsonSerializer
{
    public static T? FlagCompatibleDeserialize<T>(ReadOnlySpan<byte> data) => JsonSerializer.Deserialize<T>(data, JsonOptions.FlagCompatibleCaseInsensitive);
    
    
    public static TValue? Deserialize<TValue>(JsonDocument? document)
    {
        return document is null ? default : document.Deserialize<TValue>(JsonOptions.CaseInsensitive);
    }
}