using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using NpgsqlTypes;

namespace OpenShock.Common.OpenApi;

/// <summary>
/// Handles string enums that the schema exporter cannot describe on its own, mainly enums written by a custom converter
/// (which the exporter emits as an empty schema, dropping e.g. the <c>items</c> of a collection of them).
/// <list type="bullet">
/// <item><see cref="CreateItemsSchema"/> stands in for the dropped schema while schemas are being generated.</item>
/// <item>As a document transformer it then registers a component per enum and swaps those stand-ins for a <c>$ref</c>.
/// This cannot happen during generation, because the generator cannot unwrap a reference that has no target yet.</item>
/// <item>It also gives enum components that ended up without a <c>type</c> their <c>string</c> type.</item>
/// </list>
/// </summary>
public sealed class EnumSchemaTransformer : IOpenApiDocumentTransformer
{
    /// <summary>
    /// Stand-in schemas handed out by <see cref="CreateItemsSchema"/>, mapped to the enum they describe.
    /// </summary>
    private static readonly ConditionalWeakTable<OpenApiSchema, Type> StandIns = new();

    /// <summary>
    /// Whether <paramref name="propertyType"/> is a collection of enums, and if so which one.
    /// </summary>
    public static bool TryGetEnumElementType(Type propertyType, out Type enumType)
    {
        var element = propertyType.IsArray ? propertyType.GetElementType()
            : propertyType.IsGenericType ? propertyType.GetGenericArguments()[0]
            : null;

        enumType = element!;
        return element is { IsEnum: true };
    }

    /// <summary>
    /// Creates the schema for a string enum, named by <see cref="JsonStringEnumMemberNameAttribute"/>,
    /// then <see cref="PgNameAttribute"/>, then the member name.
    /// </summary>
    public static OpenApiSchema CreateSchema(Type enumType) => new()
    {
        Type = JsonSchemaType.String,
        Enum = enumType.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => f.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name ?? f.GetCustomAttribute<PgNameAttribute>()?.PgName ?? f.Name)
            .Select(JsonNode (name) => JsonValue.Create(name))
            .ToList()
    };

    /// <summary>
    /// Gives a string enum written by a custom converter, which comes out without a type, its <c>string</c> type.
    /// </summary>
    public static void NormalizeType(OpenApiSchema schema)
    {
        if (schema.Type is not null || schema.Enum is not { Count: > 0 } enumValues) return;

        // Nullable enums carry a real null entry, which the IList<JsonNode> annotation does not admit
        IEnumerable<JsonNode?> values = enumValues;
        if (values.Any(v => v is not null && v.GetValueKind() != JsonValueKind.String)) return;

        schema.Type = values.Any(v => v is null) ? JsonSchemaType.String | JsonSchemaType.Null : JsonSchemaType.String;
    }

    /// <summary>
    /// Creates an inline schema for use as <c>items</c> during schema generation; it is replaced by a reference in <see cref="TransformAsync"/>.
    /// </summary>
    public static OpenApiSchema CreateItemsSchema(Type enumType)
    {
        var schema = CreateSchema(enumType);
        StandIns.Add(schema, enumType);
        return schema;
    }

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var components = document.Components ??= new OpenApiComponents();
        var schemas = components.Schemas ??= new Dictionary<string, IOpenApiSchema>();

        foreach (var schema in schemas.Values.OfType<OpenApiSchema>().ToArray())
        {
            // Enums only reached through another schema (e.g. a nullable wrapper) never go through the schema transformer
            NormalizeType(schema);

            foreach (var property in schema.Properties?.Values.OfType<OpenApiSchema>() ?? [])
            {
                if (property.Items is not OpenApiSchema items || !StandIns.TryGetValue(items, out var enumType)) continue;

                schemas[enumType.Name] = CreateSchema(enumType);
                property.Items = new OpenApiSchemaReference(enumType.Name, document);
            }
        }

        return Task.CompletedTask;
    }
}
