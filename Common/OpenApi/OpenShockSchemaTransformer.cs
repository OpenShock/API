using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using OpenShock.Common.DataAnnotations;
using OpenShock.Common.DataAnnotations.Interfaces;
using OpenShock.Common.Models;
using OpenShock.Internal.Common.Problems;

namespace OpenShock.Common.OpenApi;

public sealed class OpenShockSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type == typeof(SemVersion))
        {
            ReplaceWith(schema, OpenApiSchemas.SemVerSchema);
            return Task.CompletedTask;
        }

        if (context.JsonTypeInfo.Type == typeof(IPAddress))
        {
            ReplaceWith(schema, OpenApiSchemas.IpAddressSchema);

            // The exporter has no type to derive nullability from, the converter owns the schema
            if (context.JsonPropertyInfo?.IsGetNullable == true && schema.Type is { } ipType) schema.Type = ipType | JsonSchemaType.Null;

            return Task.CompletedTask;
        }

        if (context.JsonTypeInfo.Type == typeof(PauseReason))
        {
            ReplaceWith(schema, OpenApiSchemas.PauseReasonEnumSchema);
            return Task.CompletedTask;
        }

        // OpenShockProblem.message is always populated (it mirrors the title), even though the shared type annotates it as nullable
        if (context.JsonPropertyInfo is { Name: "message", DeclaringType: var declaringType } && declaringType == typeof(OpenShockProblem) && schema.Type is { } messageType)
        {
            schema.Type = messageType & ~JsonSchemaType.Null;
        }

        StripValueTypeNullability(schema, context);
        NormalizeNumbers(schema);
        EnumSchemaTransformer.NormalizeType(schema);
        NormalizeObject(schema, context);
        ApplyMemberMetadata(schema, context);

        // Apply OpenShock Parameter Attributes
        foreach (var attribute in GetCustomAttributeProvider(context)?.GetCustomAttributes(true).OfType<IParameterAttribute>() ?? [])
        {
            attribute.Apply(schema);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// A Nullable&lt;T&gt; component shares its id with T (see <see cref="OpenShockSchemaIds"/>), so it must not describe null itself.
    /// </summary>
    private static void StripValueTypeNullability(OpenApiSchema schema, OpenApiSchemaTransformerContext context)
    {
        if (Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) is null) return;

        // Only enums and objects become shared components; a nullable primitive (int?, DateTime?) must keep its own nullability
        if (schema.Enum is null && (schema.Type is not { } objectType || (objectType & ~JsonSchemaType.Null) != JsonSchemaType.Object)) return;

        if (schema.Type is { } type) schema.Type = type & ~JsonSchemaType.Null;
        if (schema.Enum is { } values) schema.Enum = values.Where(v => (JsonNode?)v is not null).ToList();
    }

    /// <summary>
    /// Two things the exporter leaves awkward for a client:
    /// <list type="bullet">
    /// <item>JsonNumberHandling.AllowReadingFromString makes every number an "integer or numeric string" union, which would
    /// have clients type every count as <c>number | string</c>. A response always carries a number, so the string half goes.</item>
    /// <item>Unsigned formats have no OpenAPI equivalent that generators recognise, and an unrecognised format falls back to
    /// a plain number, silently losing precision past 2^53. Each becomes the narrowest signed format that covers it, with the
    /// real range as bounds. uint32 keeps no format at all: int64 would push clients to bigint for a value a double holds
    /// exactly, while int32 would misstate the upper half of its range.</item>
    /// </list>
    /// </summary>
    private static void NormalizeNumbers(OpenApiSchema schema)
    {
        if (schema.Type is not { } type) return;

        if ((type & (JsonSchemaType.Integer | JsonSchemaType.Number)) != 0 && type.HasFlag(JsonSchemaType.String))
        {
            schema.Type = type & ~JsonSchemaType.String;
            if (schema.Pattern?.StartsWith("^-?(?:0|[1-9]", StringComparison.Ordinal) == true) schema.Pattern = null;
        }

        (string? Format, string Maximum)? unsigned = schema.Format switch
        {
            "uint8" => ("int32", "255"),
            "uint16" => ("int32", "65535"),
            "uint32" => (null, "4294967295"),
            "uint64" => ("int64", "18446744073709551615"),
            _ => null
        };
        if (unsigned is not { } signed) return;

        schema.Format = signed.Format;
        // A [Range] attribute is narrower than the type's own range, so it wins.
        schema.Minimum ??= "0";
        schema.Maximum ??= signed.Maximum;
    }

    /// <summary>
    /// A property with an initializer is exported with a default and dropped from <c>required</c>, but the serializer always
    /// writes it, so a response carries it either way and a client should not have to treat it as optional.
    /// The default stays on the schema: it describes what the server assumes when a request leaves the property out.
    /// </summary>
    private static void NormalizeObject(OpenApiSchema schema, OpenApiSchemaTransformerContext context)
    {
        if (schema.Type is not { } type || (type & ~JsonSchemaType.Null) != JsonSchemaType.Object) return;
        if (context.JsonTypeInfo.Kind != JsonTypeInfoKind.Object) return;

        foreach (var (name, property) in schema.Properties ?? new Dictionary<string, IOpenApiSchema>())
        {
            if (property is not OpenApiSchema { Default: not null, Type: { } propertyType } || propertyType.HasFlag(JsonSchemaType.Null)) continue;

            (schema.Required ??= new HashSet<string>()).Add(name);
        }
    }

    /// <summary>
    /// Member-level metadata the exporter does not derive from attributes: [Obsolete] and [Required] on strings.
    /// </summary>
    private static void ApplyMemberMetadata(OpenApiSchema schema, OpenApiSchemaTransformerContext context)
    {
        if (context.JsonPropertyInfo is not { } property) return;

        // The exporter drops the item schema of collections whose element uses a custom converter (e.g. PermissionType)
        if (schema.Items is null && schema.Type is { } collectionType && collectionType.HasFlag(JsonSchemaType.Array) &&
            EnumSchemaTransformer.TryGetEnumElementType(property.PropertyType, out var enumType))
        {
            schema.Items = EnumSchemaTransformer.CreateItemsSchema(enumType);
        }

        var attributes = property.AttributeProvider?.GetCustomAttributes(true) ?? [];

        if (attributes.OfType<ObsoleteAttribute>().Any()) schema.Deprecated = true;

        if (schema.Type is { } type && type.HasFlag(JsonSchemaType.String) && schema.MinLength is null &&
            attributes.OfType<RequiredAttribute>().Any(a => !a.AllowEmptyStrings))
        {
            schema.MinLength = 1;
        }
    }

    /// <summary>
    /// Resolves the member (property/field) or controller action parameter that this schema is being generated for,
    /// which the transformer context exposes only in pieces.
    /// </summary>
    private static ICustomAttributeProvider? GetCustomAttributeProvider(OpenApiSchemaTransformerContext context)
    {
        if (context.JsonPropertyInfo?.AttributeProvider is { } attributeProvider)
        {
            return attributeProvider;
        }

        if (context.ParameterDescription?.ParameterDescriptor is ControllerParameterDescriptor { ParameterInfo: { } parameterInfo })
        {
            return parameterInfo;
        }

        return null;
    }

    /// <summary>
    /// Replaces a generated schema wholesale. A transformer can only mutate the instance it is handed, so every property
    /// a default-generated leaf schema might carry is overwritten, not just the ones the replacement sets.
    /// </summary>
    private static void ReplaceWith(OpenApiSchema schema, OpenApiSchema replacement)
    {
        schema.Type = replacement.Type;
        schema.Format = replacement.Format;
        schema.Pattern = replacement.Pattern;
        schema.Title = replacement.Title;
        schema.Description = replacement.Description;
        schema.Examples = replacement.Examples;
        schema.Enum = replacement.Enum;
        schema.Properties = replacement.Properties;
        schema.Items = replacement.Items;
        schema.AdditionalProperties = replacement.AdditionalProperties;
        schema.Required = replacement.Required;
        schema.Minimum = replacement.Minimum;
        schema.Maximum = replacement.Maximum;
        schema.MinLength = replacement.MinLength;
        schema.MaxLength = replacement.MaxLength;
    }
}
