using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace OpenShock.Common.OpenApi;

public sealed class OpenApiSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
