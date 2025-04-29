using System.Text;
using System.Text.Json.Serialization.Metadata;
using OpenShock.Common.Models;

namespace OpenShock.Common.OpenApi;

public static class OpenApiSchemaReferenceIdUtil
{
    private static bool IsCollection(Type genericDef)
        => genericDef == typeof(IEnumerable<>)
           || genericDef == typeof(IAsyncEnumerable<>)
           || genericDef == typeof(IReadOnlyList<>)
           || genericDef == typeof(IReadOnlyCollection<>)
           || genericDef == typeof(IList<>)
           || genericDef == typeof(List<>);

    public static string GetFriendlyName(Type type)
    {
        if (!type.IsGenericType)
        {
            if (type.IsArray)
            {
                return "CollectionOf" + type.GetElementType()!.Name;
            }
            
            return type.Name;
        }
        
        StringBuilder sb = new();
        
        while (type.IsGenericType)
        {
            var genericTypeDefinition = type.GetGenericTypeDefinition();

            if (genericTypeDefinition == typeof(LegacyDataResponse<>) || genericTypeDefinition == typeof(Nullable<>))
            {
                type = type.GetGenericArguments()[0];
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
    public static string GetFriendlyName(JsonTypeInfo type)
    {
        return GetFriendlyName(type.Type);
    }
}
