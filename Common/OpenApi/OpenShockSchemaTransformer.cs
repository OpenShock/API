using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
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
    /// .NET 10 models numbers as "integer or numeric string" (with a pattern) because of JsonNumberHandling.AllowReadingFromString,
    /// and uses unsigned formats. Swashbuckle emitted plain integers with signed formats, which generators understand better.
    /// </summary>
    private static void NormalizeNumbers(OpenApiSchema schema)
    {
        if (schema.Type is not { } type) return;

        if ((type & (JsonSchemaType.Integer | JsonSchemaType.Number)) != 0 && type.HasFlag(JsonSchemaType.String))
        {
            schema.Type = type & ~JsonSchemaType.String;
            if (schema.Pattern?.StartsWith("^-?(?:0|[1-9]", StringComparison.Ordinal) == true) schema.Pattern = null;
        }

        schema.Format = schema.Format switch
        {
            "uint8" or "uint16" or "uint32" => "int32",
            "uint64" => "int64",
            _ => schema.Format
        };
    }

    /// <summary>
    /// Swashbuckle marked every object schema without a dictionary value type as closed (additionalProperties: false),
    /// left extension-data types open, and did not treat property initializers as optional.
    /// </summary>
    private static void NormalizeObject(OpenApiSchema schema, OpenApiSchemaTransformerContext context)
    {
        if (schema.Type is not { } type || (type & ~JsonSchemaType.Null) != JsonSchemaType.Object) return;
        if (context.JsonTypeInfo.Kind != JsonTypeInfoKind.Object) return;

        // Properties with an initializer are exported with a default and dropped from "required", but they are always present on the wire
        foreach (var (name, property) in schema.Properties ?? new Dictionary<string, IOpenApiSchema>())
        {
            if (property is not OpenApiSchema { Default: not null, Type: { } propertyType } inline || propertyType.HasFlag(JsonSchemaType.Null)) continue;

            (schema.Required ??= new HashSet<string>()).Add(name);
            inline.Default = null;
        }

        if (schema.AdditionalProperties is not null) return;

        var hasExtensionData = context.JsonTypeInfo.Type
            .GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Any(m => m.IsDefined(typeof(JsonExtensionDataAttribute), true));

        if (hasExtensionData)
        {
            schema.AdditionalProperties = new OpenApiSchema();
        }
        else
        {
            schema.AdditionalPropertiesAllowed = false;
        }
    }

    /// <summary>
    /// Restores member-level metadata Swashbuckle derived from attributes: [Obsolete], get-only properties, and [Required] on strings.
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
        if (property is { Get: not null, Set: null } && schema.Type is not null) schema.ReadOnly = true;

        if (schema.Type is { } type && type.HasFlag(JsonSchemaType.String) && schema.MinLength is null &&
            attributes.OfType<RequiredAttribute>().Any(a => !a.AllowEmptyStrings))
        {
            schema.MinLength = 1;
        }
    }

    /// <summary>
    /// Resolves the member (property/field) or controller action parameter that this schema is being generated for,
    /// mirroring what Swashbuckle exposed as SchemaFilterContext.MemberInfo / ParameterFilterContext.ParameterInfo.
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
    /// Mimics Swashbuckle's MapType&lt;T&gt;, which fully replaces the generated schema for a type. Transformers can only
    /// mutate the schema instance in place, so every property a default-generated leaf schema might carry is reset first.
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
