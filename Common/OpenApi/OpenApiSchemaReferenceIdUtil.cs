using System.Text;
using System.Text.Json.Serialization.Metadata;
using OpenShock.Common.Models;

namespace OpenShock.Common.OpenApi;

public static class OpenApiSchemaReferenceIdUtil
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
        StringBuilder sb = new();
        
        while (type.IsGenericType)
        {
            var genericTypeDefinition = type.GetGenericTypeDefinition();

            if (genericTypeDefinition == typeof(LegacyDataResponse<>) || genericTypeDefinition == typeof(Nullable<>))
            {
                type = type.GetGenericArguments()[0];
                if (IsSystemType(type)) return null;
                continue;
            }

            if (IsCollection(genericTypeDefinition))
            {
                sb.Append("CollectionOf");
                type = type.GetGenericArguments()[0];
                continue;
            }

            if (genericTypeDefinition == typeof(Paginated<>))
            {
                sb.Append("Paginated");
                type = type.GetGenericArguments()[0];
                continue;
            }

            throw new NotImplementedException();
        }

        sb.Append(type.Name);
        
        return sb.ToString();
    }
    
    public static string? GetFriendlyName(Type type)
    {
        if (IsSystemType(type)) return null;

        if (type.IsArray) return "CollectionOf" + (GetFriendlyName(type.GetElementType()!) ?? type.Name);
        
        if (type.IsGenericType) return GetFriendlyGenericTypeName(type);
        
        return type.Name;
    }
    public static string? GetFriendlyName(JsonTypeInfo jsonTypeInfo)
    {
        return GetFriendlyName(jsonTypeInfo.Type);
    }
}
