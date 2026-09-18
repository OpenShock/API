using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using OpenShock.Common.DataAnnotations;
using OpenShock.Common.DataAnnotations.Interfaces;
using OpenShock.Common.Models;

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

        // Apply OpenShock Parameter Attributes
        foreach (var attribute in GetCustomAttributeProvider(context)?.GetCustomAttributes(true).OfType<IParameterAttribute>() ?? [])
        {
            attribute.Apply(schema);
        }

        return Task.CompletedTask;
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
