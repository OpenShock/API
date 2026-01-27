using System.Text.Json.Serialization.Metadata;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using OpenShock.Common.Models;
using OpenShock.Common.Utils;

namespace OpenShock.Common.OpenAPI;

public static class OpenApiExtensions
{
    public static IServiceCollection AddOpenApiExt<TProgram>(this WebApplicationBuilder builder) where TProgram : class
    {
        builder.Services.AddOutputCache(options =>
        {
            options.AddPolicy("OpenAPI", policy => policy.Expire(TimeSpan.FromMinutes(10)));
        });

        builder.Services.AddOpenApi("v1", options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
            options.AddDocumentTransformer(DocumentDefaults.GetDocumentTransformer(version: "1"));
            options.CreateSchemaReferenceId = GetCleanName;
            options.AddOperationTransformer(OperationTransformer);
        });

        builder.Services.AddOpenApi("v2", options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
            options.AddDocumentTransformer(DocumentDefaults.GetDocumentTransformer(version: "2"));
            options.CreateSchemaReferenceId = GetCleanName;
            options.AddOperationTransformer(OperationTransformer);
        });

        builder.Services.AddOpenApi("oauth", options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
            options.ShouldInclude = apiDescription => apiDescription.GroupName is "oauth";
            options.AddDocumentTransformer(DocumentDefaults.GetDocumentTransformer(version: "1"));
            options.CreateSchemaReferenceId = GetCleanName;
            options.AddOperationTransformer(OperationTransformer);
        });
        builder.Services.AddOpenApi("admin", options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
            options.ShouldInclude = apiDescription => apiDescription.GroupName is "admin";
            options.AddDocumentTransformer(DocumentDefaults.GetDocumentTransformer(version: "1"));
            options.CreateSchemaReferenceId = GetCleanName;
            options.AddOperationTransformer(OperationTransformer);
        });

        return builder.Services;
    }
    private static string RemoveResponseSuffix(string name)
    {
        string? value;
        if (StringUtils.TryRemoveSuffix(name, "LegacyResponse", out value)) return value;
        if (StringUtils.TryRemoveSuffix(name, "Response", out value)) return value;
        return name;
    }

    private static string? GetCleanName(JsonTypeInfo type)
    {
        if (!type.Type.IsEnum && Type.GetTypeCode(type.Type) is TypeCode.Char or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16
            or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single
            or TypeCode.Double or TypeCode.Decimal)
        {
            return null;
        }

        return GetCleanNameRecursive(type.Type);
    }

    private static string GetCleanNameRecursive(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            type = underlying;
        }
        
        if (type.IsEnum || Type.GetTypeCode(type) is not TypeCode.Object)
        {
            return type.Name;
        }

        // Handle arrays
        if (type.IsArray)
        {
            return RemoveResponseSuffix(GetCleanNameRecursive(type.GetElementType()!)) + "Array";
        }
    
        var isGeneric = type.IsGenericType;
        if (!isGeneric)
        {
            return type.Name;
        }

        var genericTypeDef = type.GetGenericTypeDefinition();
        if (genericTypeDef == typeof(List<>) ||
            genericTypeDef == typeof(IList<>) ||
            genericTypeDef == typeof(HashSet<>) ||
            genericTypeDef == typeof(IEnumerable<>) ||
            genericTypeDef == typeof(IAsyncEnumerable<>))
        {
            return RemoveResponseSuffix(GetCleanNameRecursive(type.GetGenericArguments()[0])) + "Array";
        }

        if (genericTypeDef == typeof(Dictionary<,>))
        {
            return $"DictionaryOf{GetCleanNameRecursive(type.GetGenericArguments()[0])}And{GetCleanNameRecursive(type.GetGenericArguments()[1])}";
        }


        if (genericTypeDef == typeof(LegacyDataResponse<>))
        {
            return RemoveResponseSuffix(GetCleanNameRecursive(type.GetGenericArguments()[0])) + "LegacyResponse";
        }
    
        // Handle generic types
        var genericArgs = string.Join("And", type.GetGenericArguments().Select(GetCleanNameRecursive));
    
        var name = type.Name.AsSpan();
        var backtickIndex = name.IndexOf('`');
        if (backtickIndex > 0)
        {
            name = name[..backtickIndex];
        }

        return $"{name}Of{genericArgs}";
    }

    private static Task OperationTransformer(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var actionDescriptor = context.Description.ActionDescriptor;

        // Use endpoint name if available
        var endpointName = actionDescriptor.EndpointMetadata.OfType<EndpointNameMetadata>()
            .FirstOrDefault()
            ?.EndpointName;

        if (!string.IsNullOrEmpty(endpointName))
        {
            operation.OperationId = endpointName;
            return Task.CompletedTask;
        }

        // For controllers
        var controller = actionDescriptor.RouteValues.TryGetValue("controller", out var ctrl) ? ctrl : null;
        var action = actionDescriptor.RouteValues.TryGetValue("action", out var act) ? act : null;

        if (!string.IsNullOrEmpty(controller) && !string.IsNullOrEmpty(action))
        {
            operation.OperationId = $"{controller}{action}";
        }

        return Task.CompletedTask;
    }
}