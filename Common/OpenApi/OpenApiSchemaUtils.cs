using Microsoft.AspNetCore.OpenApi;
using OpenShock.Common.Models;
using System.Text.Json.Serialization.Metadata;

namespace OpenShock.Common.OpenApi;

public static class OpenApiSchemaUtils
{
    private static readonly HashSet<Type> CollectionTypes =
    [
        typeof(List<>),
        typeof(IList<>),
        typeof(IReadOnlyList<>),
        typeof(IEnumerable<>),
        typeof(IAsyncEnumerable<>),
        typeof(IReadOnlyCollection<>)
    ];
    
    private static bool IsCollection(Type genericDef) => CollectionTypes.Contains(genericDef);

    private static bool IsSystemType(Type type)
    {
        if (Type.GetTypeCode(type) is not (TypeCode.Empty or TypeCode.Object or TypeCode.DBNull))
            return true;
        
        return type == typeof(Guid) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(Uri);
    }
    
    private static string? GetFriendlyGenericTypeName(Type type)
    {
        string suffix = "";

        while (type.IsGenericType || type.IsArray)
        {
            if (type.IsArray)
            {
                suffix = "Array" + suffix;
                type = type.GetElementType()!;
                continue;
            }

            var genericTypeDefinition = type.GetGenericTypeDefinition();

            if (genericTypeDefinition == typeof(LegacyDataResponse<>) || genericTypeDefinition == typeof(Nullable<>))
            {
                type = type.GetGenericArguments()[0];
                if (IsSystemType(type)) return null;
                continue;
            }

            if (IsCollection(genericTypeDefinition))
            {
                suffix = "Array" + suffix;
                type = type.GetGenericArguments()[0];
                continue;
            }

            if (genericTypeDefinition == typeof(Paginated<>))
            {
                suffix = "Page" + suffix;
                type = type.GetGenericArguments()[0];
                continue;
            }

            throw new NotImplementedException();
        }

        return type.Name + suffix;
    }
    
    private static string? GetFriendlyName(Type type)
    {
        if (IsSystemType(type)) return null;

        
        if (type.IsGenericType || type.IsArray) return GetFriendlyGenericTypeName(type);

        return type.Name;
    }
    

    public static void ConfigureOptions(OpenApiOptions options)
    {
        options.CreateSchemaReferenceId = (jsonTypeInfo) => GetFriendlyName(jsonTypeInfo.Type);

        options.AddDocumentTransformer<OpenApiDocumentTransformer>();
        options.AddOperationTransformer<OpenApiOperationTransformer>();
    }
}
