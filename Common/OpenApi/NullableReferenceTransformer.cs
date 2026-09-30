using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace OpenShock.Common.OpenApi;

/// <summary>
/// A nullable reference to a component is exported as <c>oneOf: [null, $ref]</c>. Swashbuckle emitted the bare <c>$ref</c>,
/// so generated clients keep their non-nullable types. Wrappers that carry their own metadata are left alone.
/// </summary>
public sealed class NullableReferenceTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        foreach (var schema in document.Components?.Schemas?.Values.OfType<OpenApiSchema>().ToArray() ?? [])
        {
            if (schema.Properties is not { } properties) continue;

            foreach (var (name, property) in properties.ToArray())
            {
                if (property is not OpenApiSchema { OneOf: { Count: 2 } oneOf, Description: null } wrapper) continue;
                if (wrapper.Type is not null && wrapper.Type != JsonSchemaType.Null) continue;

                var reference = oneOf.OfType<OpenApiSchemaReference>().SingleOrDefault();
                if (reference is null || !oneOf.OfType<OpenApiSchema>().Any(IsNullSchema)) continue;

                properties[name] = reference;
            }
        }

        return Task.CompletedTask;
    }

    private static bool IsNullSchema(OpenApiSchema schema)
    {
        if (schema.Type == JsonSchemaType.Null) return true;

        IEnumerable<JsonNode?>? values = schema.Enum;
        return values is not null && values.Count() == 1 && values.Single() is null;
    }
}
