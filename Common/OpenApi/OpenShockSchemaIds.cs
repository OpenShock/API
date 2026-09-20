using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;
using OpenShock.Common.Models;

namespace OpenShock.Common.OpenApi;

/// <summary>
/// Schema ids matching what Swashbuckle produced, so generated clients keep their type names:
/// generic arguments come first (<c>LegacyDataResponse&lt;bool&gt;</c> is <c>BooleanLegacyDataResponse</c>) and arrays end in "Array".
/// </summary>
public static class OpenShockSchemaIds
{
    public static string? Create(JsonTypeInfo typeInfo)
    {
        var type = typeInfo.Type;

        // Inlined in the old documents
        if (type == typeof(SemVersion) || type == typeof(PauseReason)) return null;

        var id = OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo);
        if (id is null) return null;

        return type.IsArray || type.IsGenericType ? GetName(type) : id;
    }

    private static string GetName(Type type)
    {
        while (true)
        {
            // T? shares T's schema; nullability is not part of the id
            if (Nullable.GetUnderlyingType(type) is not { } underlying)
            {
                if (type.IsArray) return GetName(type.GetElementType()!) + "Array";
                if (!type.IsGenericType) return type.Name;

                var name = type.Name;
                return string.Concat(type.GetGenericArguments().Select(GetName)) + name[..name.IndexOf('`')];
            }

            type = underlying;
        }
    }
}
