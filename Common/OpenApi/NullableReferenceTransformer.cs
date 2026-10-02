using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace OpenShock.Common.OpenApi;

/// <summary>
/// A nullable reference to a component is exported as <c>oneOf: [null, $ref]</c>, which client generators read as a union
/// with an unknown member rather than a nullable reference; Swashbuckle emitted the bare <c>$ref</c>, which instead lost
/// the nullability. Rewrites both into the 3.0 idiom, <c>nullable: true</c> next to <c>allOf: [$ref]</c>, so generated
/// clients type the property as nullable and null-check it before recursing into it.
/// Wrappers that carry their own metadata are left alone.
/// </summary>
public sealed class NullableReferenceTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        foreach (var schema in document.Components?.Schemas?.Values.OfType<OpenApiSchema>().ToArray() ?? [])
        {
            if (schema.Properties is not { } properties) continue;

            foreach (var property in properties.Values.ToArray())
            {
                if (property is not OpenApiSchema { OneOf: { Count: 2 } oneOf, Description: null } wrapper) continue;
                if (wrapper.Type is not null && wrapper.Type != JsonSchemaType.Null) continue;

                // Exactly one of the two members has to be the reference and the other the null, or this is a
                // union that means something of its own.
                if (oneOf.OfType<OpenApiSchemaReference>().ToArray() is not [var reference]) continue;
                if (!oneOf.OfType<OpenApiSchema>().Any(IsNullSchema)) continue;

                // A bare Null type serializes to `nullable: true` alone; the reference has to sit under allOf,
                // because 3.0 forbids sibling keywords next to a $ref.
                wrapper.OneOf = null;
                wrapper.AllOf = [reference];
                wrapper.Type = JsonSchemaType.Null;
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
