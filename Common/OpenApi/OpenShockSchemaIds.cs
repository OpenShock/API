using System.Net;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;
using OpenShock.Common.Models;

namespace OpenShock.Common.OpenApi;

/// <summary>
/// Schema ids are the generator's own. Three types are inlined instead of becoming components: each is written by a
/// custom converter as a single scalar and gets its schema from <see cref="OpenShockSchemaTransformer"/>, so a named
/// component would only add a wrapper for clients to unwrap.
/// </summary>
public static class OpenShockSchemaIds
{
    public static string? Create(JsonTypeInfo typeInfo)
    {
        var type = typeInfo.Type;

        if (type == typeof(SemVersion) || type == typeof(PauseReason) || type == typeof(IPAddress)) return null;

        return OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo);
    }
}
